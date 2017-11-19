using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StatisticsAnalyzerCore.Modeling;

namespace DesktopApp
{
    public partial class VariablePane : UserControl
    {
        private MixedLinearModel _model;
        private ListBox _draggedListBox;
        private Dictionary<string, ModelVariable> _variables; 

        public delegate void FormulaChanged(string formula);
        public event FormulaChanged HandleFormulaChanged;

        private void OnHandleFormulaChanged()
        {
            var sb = new StringBuilder();

            sb.Append(predictedLst.Items[0]);
            sb.Append("~");
            
            var fixedEffects = new List<string>();
            for (int i = 0; i < fixedList.Items.Count; i++)
            {
                fixedEffects.Add(fixedList.Items[i].ToString());
            }
            sb.Append(string.Join("+", fixedEffects));

            Action<bool, string, List<string>, StringBuilder> addRandomGroup =
                (run, randPivot, randCov, b) =>
                {
                    if (run)
                    {
                        sb.Append("+");
                        sb.Append("(");
                        sb.Append(string.Join("+", randCov));
                        sb.Append("|");
                        sb.Append(randPivot);
                        sb.Append(")");
                    }
                };

            bool hasVariable = false;
            List<string> covariates = null;
            string randoPivot = "";
            foreach (object t in randomList.Items)
            {
                if (!t.ToString().StartsWith(" "))
                {
                    addRandomGroup(hasVariable, randoPivot, covariates, sb);
                    randoPivot = t.ToString();
                    covariates = new List<string>();
                    hasVariable = true;
                }
                else
                {
                    var covariate = t.ToString().TrimStart(' ');
                    if (covariates != null) covariates.Add(covariate);
                }
            }
            addRandomGroup(hasVariable, randoPivot, covariates, sb);

            var formula = new MixedLinearModel(sb.ToString());
            if (HandleFormulaChanged != null) HandleFormulaChanged(formula.ModelFormula);
        }

        public string Model
        {
            set
            {
                _model = new MixedLinearModel(value);
                fixedList.Items.Clear();
                randomList.Items.Clear();
                predictedLst.Items.Clear();

                fixedList.Items.AddRange(_model.FixedEffectVariables.Cast<object>().ToArray());
                predictedLst.Items.Add(_model.PredictedVariable);

                foreach (var randomEffectVariable in _model.RandomEffectVariables)
                {
                    foreach (var randomFormula in _model.GetRandomLinearFormulas(randomEffectVariable))
                    {
                        randomList.Items.Add(randomEffectVariable);
                        foreach (var item in randomFormula.AllVariables)
                        {
                            randomList.Items.Add("  " + item);                            
                        }
                    }
                }
            }
        }

        public List<ModelVariable> Variables
        {
            set
            {
                allVarList.Items.Clear();
                allVarList.Items.AddRange(value.Select(x => x.Name).Cast<object>().ToArray());

                _variables = value.ToDictionary(v => v.Name, v => v);
            }
        }

        public VariablePane()
        {
            InitializeComponent();
            fixedList.AllowDrop = true;
            randomList.AllowDrop = true;
            predictedLst.AllowDrop = true;
            allVarList.AllowDrop = true;

            fixedList.MouseDown += ListMouseDown;
            randomList.MouseDown += ListMouseDown;
            allVarList.MouseDown += ListMouseDown;
            fixedList.DragOver += ListDragOver;
            fixedList.DragDrop += ListDragDrop;
            randomList.DragOver += ListDragOver;
            randomList.DragDrop += ListDragDrop;
            predictedLst.DragOver += ListDragOver;
            predictedLst.DragDrop += ListDragDrop;
            allVarList.DragOver += ListDragOver;
        }

        private void HandleContextMenuOptions(object sender,
                                              int suggestedCovariateCount,
                                              int covariateCount,
                                              string item)
        {
            if (sender == randomList)
            {
                removeToolStripMenuItem.Enabled = true;

                // Apply only if some convariates are available
                addCovariateToolStripMenuItem.Enabled = suggestedCovariateCount > 0;

                // Apply covariance only if you some covariate interaction
                removeCovarianceToolStripMenuItem.Enabled = (covariateCount > 0 && item != "  1");

                // Don't remove 1/0 covarite item
                removeToolStripMenuItem.Enabled = !item.StartsWith("  1") && !item.StartsWith("  0");

                // No interactions in random part
                addInteractionToolStripMenuItem.Enabled = false;
                removeInteractionToolStripMenuItem.Enabled = false;
            }

            if (sender == fixedList)
            {
                removeToolStripMenuItem.Enabled = true;

                // Validate if interaction options are present
                addInteractionToolStripMenuItem.Enabled = fixedList.SelectedItems.Count > 1;
                removeInteractionToolStripMenuItem.Enabled = fixedList.SelectedItems.Cast<string>().Any(v => v.Contains("*"));

                // No covariates in fixed part
                removeCovarianceToolStripMenuItem.Enabled = false;
                addCovariateToolStripMenuItem.Enabled = false;
            }
        }

        private ListBox _contextMenuContext;
        private void HandleContextMenu(object sender, MouseEventArgs e)
        {
            //select the item under the mouse pointer
            var fixedIndex = fixedList.IndexFromPoint(e.Location);
            var randomIndex = randomList.IndexFromPoint(e.Location);

            Action<ListBox, int> popContextMenu = (listBox, listBoxIndex) =>
            {
                if (listBox.SelectedIndex == -1) listBox.SelectedIndex = listBoxIndex;
                contextMenuStrip1.Show();
            };

            if (fixedIndex != -1)
            {
                _contextMenuContext = fixedList;
                popContextMenu(fixedList, fixedIndex);
            }
            if (randomIndex != -1)
            {
                _contextMenuContext = randomList;
                popContextMenu(randomList, randomIndex);
            }

            addCovariateToolStripMenuItem.DropDownItems.Clear();
            if (randomIndex != -1)
            {
                var randomPivot = randomList.Items[randomIndex].ToString();
                var convariateCount = 0;
                int i;
                if (randomList.Items[randomIndex].ToString().StartsWith(" "))
                {
                    i = randomIndex;
                    while (randomList.Items[i].ToString().StartsWith(" ")) i--;
                    var pivotVariableIndex = i;

                    i = randomIndex;
                    while (randomList.Items.Count > i
                        && randomList.Items[i].ToString().StartsWith(" ")) i++;
                    var nextPivotVariableIndex = i;

                    convariateCount = nextPivotVariableIndex - pivotVariableIndex - 1;
                    randomPivot = randomList.Items[pivotVariableIndex].ToString();
                }

                var suggestedGroupedVars =
                    _variables.Keys
                        .Except(_model.GetRandomLinearFormula(randomPivot).AllVariables)
                        .Where(v => _variables[v].Type == FieldType.StringOrDecimal &&
                                    _model.PredictedVariable != v)
                        .ToList();

                i = 0;
                foreach (var suggestedGroupedVar in suggestedGroupedVars)
                {
                    addCovariateToolStripMenuItem.DropDownItems.Add(suggestedGroupedVar);
                    addCovariateToolStripMenuItem.DropDownItems[i++].Click += AddCovariate;
                }

                HandleContextMenuOptions(sender,
                                         suggestedGroupedVars.Count,
                                         convariateCount,
                                         randomList.Items[randomIndex].ToString());
            }
            else
            {
                HandleContextMenuOptions(sender, 0, 0, fixedList.Items[fixedIndex].ToString());
            }
        }

        private void ListMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                HandleContextMenu(sender, e);
                return;
            }

            var listBox = (ListBox)sender;
            if (listBox.Items.Count == 0)
                return;

            _draggedListBox = listBox;
            int index = listBox.IndexFromPoint(e.X, e.Y);
            if (index < 0) return;

            string s = listBox.Items[index].ToString();
            DragDropEffects dde1 = DoDragDrop(s, DragDropEffects.All);

            if (dde1 == DragDropEffects.All)
            {
                listBox.Items.RemoveAt(listBox.IndexFromPoint(e.X, e.Y));
                OnHandleFormulaChanged();
            }
        }

        private void ListDragOver(object sender, DragEventArgs e)
        {
            if (_draggedListBox != sender)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void ListDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var str = (string)e.Data.GetData(DataFormats.StringFormat);

                if (sender == fixedList)
                {
                    fixedList.Items.Add(str);                    
                }

                if (sender == randomList)
                {
                    randomList.Items.Add(str);
                    randomList.Items.Add("  1");
                }

                if (sender == predictedLst)
                {
                    predictedLst.Items.Clear();
                    predictedLst.Items.Add(str);
                }
            }
        }

        private void Remove(object sender, EventArgs e)
        {
            if (_contextMenuContext != null)
            {
                int index = _contextMenuContext.SelectedIndex;
                var item = _contextMenuContext.Items[index].ToString();
                _contextMenuContext.Items.RemoveAt(index);
                if (_contextMenuContext == randomList && !item.StartsWith(" "))
                {
                    while (randomList.Items.Count > index &&
                           randomList.Items[index].ToString().StartsWith(" "))
                    {
                        _contextMenuContext.Items.RemoveAt(index);                        
                    }
                }
                OnHandleFormulaChanged();                
            }
        }

        private void AddInteraction(object sender, EventArgs e)
        {
            var indices = fixedList.SelectedIndices.Cast<int>().ToList();
            var term = string.Join("*", indices.Select(i => fixedList.Items[i]));
            foreach (var index in indices.OrderByDescending(x => x))
            {
                fixedList.Items.RemoveAt(index);
            }

            fixedList.Items.Add(term);
            OnHandleFormulaChanged();
        }

        private void RemoveInteraction(object sender, EventArgs e)
        {
            var terms = fixedList.SelectedItem
                                 .ToString()
                                 .Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim());
            fixedList.Items.RemoveAt(fixedList.SelectedIndex);

            foreach (var term in terms)
            {
                fixedList.Items.Add(term);                
            }

            OnHandleFormulaChanged();
        }

        private void AddCovariate(object sender, EventArgs e)
        {
            var item = (ToolStripDropDownItem)sender;

            var randomIndex = randomList.SelectedIndex;
            while (randomList.Items[randomIndex].ToString().StartsWith(" ")) randomIndex--;
            var randomGroup = randomList.Items[randomIndex].ToString();
            var itms = randomList.Items.Cast<string>().ToList();

            var groupAdded = false;
            var covAdded = false;
            randomList.Items.Clear();
            foreach (var itm in itms)
            {
                if (!itm.StartsWith(" ") && groupAdded && !covAdded)
                {
                    randomList.Items.Add("  " + item);
                    covAdded = true;
                }
                if (itm == randomGroup) groupAdded = true;
                randomList.Items.Add(itm);
            }
            if (!covAdded) randomList.Items.Add("  " + item);

            OnHandleFormulaChanged();
        }

        private void RemoveCovariance(object sender, EventArgs e)
        {
            var randomIndex = randomList.SelectedIndex;
            var covariate = randomList.Items[randomIndex].ToString();
            while (randomList.Items[randomIndex].ToString().StartsWith(" ")) randomIndex--;
            var randomGroup = randomList.Items[randomIndex].ToString();
            var itms = randomList.Items.Cast<string>().ToList();

            var groupAdded = false;
            var newGroupAdded = false;
            var inCurrentGroup = false;
            randomList.Items.Clear();
            foreach (var itm in itms)
            {
                if (!itm.StartsWith(" ") && groupAdded && !newGroupAdded)
                {
                    randomList.Items.Add(randomGroup);
                    randomList.Items.Add("  0");
                    randomList.Items.Add(covariate);
                    newGroupAdded = true;
                    if (inCurrentGroup) inCurrentGroup = false;
                }
                if (itm == randomGroup)
                {
                    groupAdded = true;
                    inCurrentGroup = true;
                }

                if (!inCurrentGroup || itm != covariate) randomList.Items.Add(itm);
            }
            if (!newGroupAdded)
            {
                randomList.Items.Add(randomGroup);
                randomList.Items.Add("  0");
                randomList.Items.Add(covariate);
            }

            OnHandleFormulaChanged();
        }
    }
}
