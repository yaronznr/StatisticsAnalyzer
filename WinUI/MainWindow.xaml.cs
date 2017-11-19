using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using ExcelLib;
using System.Data;
using StatisticsAnalyzerCore;
using RLib;
using System.Collections.ObjectModel;

namespace WinUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable _dataTable;

        private ObservableCollection<string> _fixedEffectList;
        private ObservableCollection<string> _randomEffectList;

        /// <summary>
        /// Gets or sets whether a file was selected
        /// </summary>
        public static readonly DependencyProperty IsFileSelectedProperty = DependencyProperty.Register("IsFileSelected", typeof(Boolean), typeof(MainWindow));
        public bool IsFileSelected
        {
            get { return (bool)GetValue(IsFileSelectedProperty); }
            set { SetValue(IsFileSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether a file was selected
        /// </summary>
        public static readonly DependencyProperty DataSetColumnsProperty = DependencyProperty.Register("DataSetColumns", typeof(IEnumerable<string>), typeof(MainWindow));
        public IEnumerable<string> DataSetColumns
        {
            get { return (IEnumerable<string>)GetValue(DataSetColumnsProperty); }
            set { SetValue(DataSetColumnsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the model building current step
        /// </summary>
        public static readonly DependencyProperty CurrentStepProperty = DependencyProperty.Register("CurrentStep", typeof(int), typeof(MainWindow));
        public int CurrentStep
        {
            get { return (int)GetValue(CurrentStepProperty); }
            set { SetValue(CurrentStepProperty, value); }
        }

        /// <summary>
        /// Gets or sets the fixed effects
        /// </summary>
        public static readonly DependencyProperty FixedEffectsProperty = DependencyProperty.Register("FixedEffects", typeof(ObservableCollection<string>), typeof(MainWindow));
        public ObservableCollection<string> FixedEffects
        {
            get { return (ObservableCollection<string>)GetValue(FixedEffectsProperty); }
            set { SetValue(FixedEffectsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ranodom effects
        /// </summary>
        public static readonly DependencyProperty RandomEffectsProperty = DependencyProperty.Register("RandomEffects", typeof(ObservableCollection<string>), typeof(MainWindow));
        public ObservableCollection<string> RandomEffects
        {
            get { return (ObservableCollection<string>)GetValue(RandomEffectsProperty); }
            set { SetValue(RandomEffectsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the perdicted value
        /// </summary>
        public static readonly DependencyProperty PerdictedValueProperty = DependencyProperty.Register("PerdictedValue", typeof(string), typeof(MainWindow));
        public string PerdictedValue
        {
            get { return (string)GetValue(PerdictedValueProperty); }
            set { SetValue(PerdictedValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the perdicted value
        /// </summary>
        public static readonly DependencyProperty FixedGroupsProperty = DependencyProperty.Register("FixedGroups", typeof(ObservableCollection<ObservableCollection<string>>), typeof(MainWindow));
        public ObservableCollection<ObservableCollection<string>> FixedGroups
        {
            get { return (ObservableCollection<ObservableCollection<string>>)GetValue(FixedGroupsProperty); }
            set { SetValue(FixedGroupsProperty, value); }
        }


        /// <summary>
        /// Gets or sets the perdicted value
        /// </summary>
        public static readonly DependencyProperty RandomGroupsProperty = DependencyProperty.Register("RandomGroups", typeof(ObservableCollection<ObservableCollection<string>>), typeof(MainWindow));
        public ObservableCollection<ObservableCollection<string>> RandomGroups
        {
            get { return (ObservableCollection<ObservableCollection<string>>)GetValue(RandomGroupsProperty); }
            set { SetValue(RandomGroupsProperty, value); }
        }

        public static readonly DependencyProperty IsBuildingModelProperty = DependencyProperty.Register("IsBuildingModel", typeof(bool), typeof(MainWindow));
        public bool IsBuildingModel
        {
            get { return (bool)GetValue(IsBuildingModelProperty); }
            set { SetValue(IsBuildingModelProperty, value); }
        }

        public static readonly DependencyProperty ModelResultProperty = DependencyProperty.Register("ModelResult", typeof(string), typeof(MainWindow));
        public string ModelResult
        {
            get { return (string)GetValue(ModelResultProperty); }
            set { SetValue(ModelResultProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            modelCreationTabControl.SelectedIndex = 4;
            IsBuildingModel = false;
            DataContext = this;
        }

        #region Text Operations
        private void Bold_Checked(object sender, RoutedEventArgs e)
        {
            textBox1.FontWeight = FontWeights.Bold;
        }
        private void Bold_Unchecked(object sender, RoutedEventArgs e)
        {
            textBox1.FontWeight = FontWeights.Normal;
        }
        private void Italic_Checked(object sender, RoutedEventArgs e)
        {
            textBox1.FontStyle = FontStyles.Italic;
        }
        private void Italic_Unchecked(object sender, RoutedEventArgs e)
        {
            textBox1.FontStyle = FontStyles.Normal;
        }
        private void IncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.FontSize < 18)
            {
                textBox1.FontSize += 2;
            }
        }
        private void DecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.FontSize > 10)
            {
                textBox1.FontSize -= 2;
            }
        }
        #endregion Text Operations

        #region File Operations
        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void CloseFile(object sender, RoutedEventArgs e)
        {
            // Mark file as selected
            IsFileSelected = false;
        }
        private void LoadFile(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Load Excel file
                var excelFile = new ExcelDocument(fileDialog.FileName);
                _dataTable = excelFile.LoadCellData();
                this.dataGrid1.ItemsSource = _dataTable.DefaultView;

                // Bind columns
                List<string> columns = new List<string>();
                foreach (var col in _dataTable.Columns)
                {
                    columns.Add(col.ToString());
                }
                DataSetColumns = columns;

                // Set some UI values
                this.btnBuildModel.IsEnabled = false;
                this.textBox1.Text = "Select comparision to be made...";
                this.textBox1.IsEnabled = false;
                
                // Mark file as selected
                IsFileSelected = true;
            }

        }
        #endregion File Operations

        #region Model Steps
        private void MoveStepBack(object sender, RoutedEventArgs e)
        {
            if (CurrentStep == 5)
            {
                CurrentStep = 1;
            }
            else
            {
                this.CurrentStep = Math.Max(this.CurrentStep - 1, 0);
            }
        }

        private void MoveStepNext(object sender, RoutedEventArgs e)
        {
            if (CurrentStep == 1)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                ModelResult = /*RHelper.RunMixedModel(_dataTable, txtMixedEffectFormula.Text);*/ string.Empty;
                Mouse.OverrideCursor = null;
                CurrentStep = 5;
            }
            else
            {
                this.CurrentStep = Math.Min(this.CurrentStep + 1, 5);
                var tabItem1 = modelCreationTabControl.Items[this.CurrentStep] as TabItem;
                tabItem1.Visibility = Visibility.Visible;
            }
        }
        #endregion Model Steps

        #region Model Edits
        private void SwitchRandonFixedEffect(object sender, RoutedEventArgs e)
        {
            if (sender == btnToFixedEffect || sender == lstFixedEffects)
            {
                var item = lstFixedEffects.SelectedItem as string;
                FixedEffects.Remove(item);
                RandomEffects.Add(item);
            }
            else if (sender == btnToRandomEffect || sender == lstRandomEffects)
            {
                var item = lstRandomEffects.SelectedItem as string;
                RandomEffects.Remove(item);
                FixedEffects.Add(item);
            }

            EffectsUpdated();
        }

        #endregion Model Edits

        private void PerdictedVariableSelected(object sender, RoutedEventArgs e)
        {
            btnBuildModel.IsEnabled = true;
        }

        private void StartBuildModel(object sender, RoutedEventArgs e)
        {
            FixedEffects = new ObservableCollection<string>();
            RandomEffects = new ObservableCollection<string>();

            var analyzer = new DatasetAnalyzer(_dataTable);
            foreach (DataColumn column in _dataTable.Columns)
            {
                if (column.ColumnName != PerdictedValue)
                {
                    if (analyzer.IsDiscrete(column.ColumnName))
                    {
                        RandomEffects.Add(column.ColumnName);
                    }
                    else
                    {
                        FixedEffects.Add(column.ColumnName);
                    }
                }
            }

            EffectsUpdated();

            var tabItem1 = modelCreationTabControl.Items[0] as TabItem;
            tabItem1.Visibility = Visibility.Visible;
            CurrentStep = 0;

            IsBuildingModel = true;

            MixedLinearModel mixedModel = new MixedLinearModel(FixedEffects, RandomEffects, PerdictedValue);
            txtMixedEffectFormula.Text = mixedModel.ModelFormula;
        }

        private void EffectsUpdated()
        {
            FixedGroups = new ObservableCollection<ObservableCollection<string>>(FixedEffects.Select(i => new ObservableCollection<string>(new List<string> { i })));
            RandomGroups = new ObservableCollection<ObservableCollection<string>>(RandomEffects.Select(i => new ObservableCollection<string>(new List<string> { "1" })));
        }
    }
}
