using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DesktopApp.Properties;
using Microsoft.Win32;
using REngine;
using ServicesLib;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.Questions;

namespace DesktopApp
{
    public partial class MainForm : Form
    {
        private delegate void CloseFormCallback();
        public MainForm()
        {
            InitializeComponent();
            var interactiveR = new InteractiveR();
            ServiceContainer.RService().InteractiveR = interactiveR;
            rConsole.InteractiveRConsole = interactiveR;

            if (!File.Exists("Loaded"))
            {
                var form = new LoadingForm
                {
                    Text = Resources.MainForm_MainForm_Loading_R_packages_for_the_first_time___,
                    TopMost = true,
                };
                // ReSharper disable once LocalizableElement

                Action action = () =>
                {
                    // HKLM\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                                      AppDomain.CurrentDomain.FriendlyName,
                                      0x2af9);
                    string error;
                    var path = RWindowsHelper.GetRPathBase().Replace("\\", "/");
                    var prms = "repos = NULL, type='binary'";
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/pixmap_0.4-11.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/tkrplot_0.0-23.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/multcompView_0.1-5.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/plotrix_3.5-12.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/deSolve_1.11.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/mvtnorm_1.0-2.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/rJava_0.9-6.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/scatterplot3d_0.3-35.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/XLConnectJars_0.2-9.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/SparseM_1.6.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/minqa_1.2.4.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/Rcpp_0.11.6.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/nloptr_1.0.4.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/XLConnect_0.2-11.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/pbkrtest_0.4-2.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/multcomp_1.4-0.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/lme4_1.1-7.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/influence.ME_0.9-5.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/car_2.0-25.zip', {1})", path, prms), out error, false);
                    interactiveR.RunRCommand(string.Format("install.packages('{0}/external/asbio_1.1-5.zip', {1})", path, prms), out error, false);
                    var d = new CloseFormCallback(form.Close);
                    Invoke(d);
                    File.Create("Loaded").Close();
                };

                action.BeginInvoke(action.EndInvoke, null);
                form.Show();
            }
        }

        private void LoadDatasetPage()
        {
            var sb = new StringBuilder();

            var path = Directory.GetCurrentDirectory();

            sb.AppendLine(string.Format("<script type=\"text/javascript\" src=\"file://{0}/bin/Scripts/app/mixed.rules.js\"></script>", path));
            sb.AppendLine(string.Format("<script type=\"text/javascript\" src=\"file://{0}/bin/Scripts/jslib/jquery-1.8.2.min.js\"></script>", path));
            sb.AppendLine(string.Format("<script type=\"text/javascript\" src=\"file://{0}/bin/Scripts/jslib/knockout-2.2.0.js\"></script>", path));
            sb.AppendLine("<link rel=\"stylesheet\" href=\"C:/Users/ziner_000/Desktop/thesis/AutomaticStatisticsAnalyzer/WebApp/Content/Site.css\" />");
            sb.AppendLine("<div id=\"questionPanelDesktop\" class=\"bottomPaneDesktop\">");
            sb.AppendLine("    <div id=\"questionList\" class=\"scrollable bottomPaneDesktop leftPane\" data-bind=\"foreach: questions\">");
            sb.AppendLine("          <div class=\"questionItem\" data-bind=\"text: question, css: { selectQuestionItem: selected }, click: selectQuestion\"></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div id=\"answersPane\" class=\"bottomPaneDesktop rightPaneDesktop\">");
            sb.AppendLine("        <div id=\"answerPane\" data-bind=\"html: selectedAnswer\" class=\"scrollable bottomPaneDesktop rightPaneDesktop\"></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<script>");
            sb.AppendLine("var viewModel = (function () {");
            sb.AppendLine("    var self = this;");
            sb.AppendLine("    self.questions = ko.observableArray();");
            sb.AppendLine("    self.selectedAnswer = ko.computed(function () {");
            sb.AppendLine("        for (var i = 0; i < self.questions().length; i++) {");
            sb.AppendLine("            if (self.questions()[i].selected()) {");
            sb.AppendLine("                return self.questions()[i].answer;");
            sb.AppendLine("            };");
            sb.AppendLine("        };");
            sb.AppendLine("    });");
            sb.AppendLine("    self.selectQuestion = function (item) {");
            sb.AppendLine("        for (var i = 0; i < self.questions().length; i++) {");
            sb.AppendLine("            self.questions()[i].selected(false);");
            sb.AppendLine("        };");
            sb.AppendLine("        item.selected(true);");
            sb.AppendLine("    };");
            sb.AppendLine("    var viewModel = { questions: questions, selectedAnswer: selectedAnswer};");
            sb.AppendLine("    return viewModel;");
            sb.AppendLine("})();");
            sb.AppendLine("");
            sb.AppendLine("var addQuestion = function (question, answer, selected) {");
            sb.AppendLine("    viewModel.questions.push({question: ko.observable(question), selected: ko.observable(selected), answer: answer });");
            sb.AppendLine("};");
            sb.AppendLine("");
            sb.AppendLine("var clearQuestions = function () {");
            sb.AppendLine("    viewModel.questions([]);");
            sb.AppendLine("};");
            sb.AppendLine("");
            sb.AppendLine("viewModel.questions.push({question: ko.observable('question1'), selected: ko.observable(true), answer: 'answer1' });");
            sb.AppendLine("viewModel.questions.push({question: ko.observable('question2'), selected: ko.observable(false), answer: 'answer2' });");
            sb.AppendLine("");
            sb.AppendLine("ko.applyBindings(viewModel);");
            sb.AppendLine("</script>");

            mainWebBrowser.DocumentText = sb.ToString();
        }

        private void ClearQuestions()
        {
            if (mainWebBrowser.Document != null) mainWebBrowser.Document.InvokeScript("clearQuestions");
        }

        private void AddQuestion(string question, string answer, bool selected)
        {
            if (mainWebBrowser.Document != null)
            {
                mainWebBrowser.Document.InvokeScript("addQuestion",
                                                     new object[] { question, answer, selected });
            }
        }

        private void AddQuestions(Dictionary<string, string> questionsDictionary, bool clearQuestions)
        {
            var firstQuestion = !clearQuestions;
            foreach (var question in questionsDictionary)
            {
                AddQuestion(question.Key, question.Value, firstQuestion);
                firstQuestion = false;
            }
        }

        private void HandleNewModelSummary(MixedLinearModel model, ModelDataset dataset)
        {
            var modelTandlation = ModelAnalyzer.TranslateModel(model, dataset);
            ClearQuestions();
            AddQuestion("Model Summary",
                        string.Join(string.Empty,
                                    modelTandlation.Where(k => k.Key != "Random Effect")
                                                   .Select(kvp => string.Format("<h2>{0}</h2>", kvp.Key) + kvp.Value)),
                        true);

            if (!string.IsNullOrEmpty(modelTandlation["Model Intent"]))
            {
                AddQuestion("Did I mean this model?", modelTandlation["Model Intent"], false);                
            }
        }

        private void HandleModelFormulaChanged(string formula)
        {
            modelFormulaText.Text = formula;

            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument("Temp");
            var model = ServiceContainer.ModelService().GetModel(modelFormulaText.Text);
            HandleNewModelSummary(model, dataset);
        }

        private void OpenNewFile(object sender, EventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var fileName = dialog.FileName;
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(fileName, FileMode.Open))
                {
                    fileStream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                }

                ServiceContainer.StorageService().UploadExcel("Temp",
                                                              Path.GetFileName(fileName),
                                                              memoryStream);

                var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument("Temp");
                var model = ServiceContainer.ModelService().GetModel(null, dataset, "Temp", Path.GetFileName(fileName));
                var modelObj = ServiceContainer.ModelService().GetModel(model.ModelFormula, "Temp");
                Analysis = null;

                LoadDatasetPage();
                mainWebBrowser.DocumentCompleted += (aa, bb) => HandleNewModelSummary(model, dataset);

                modelFormulaText.Text = model.ModelFormula;
                variablePane.Model = model.ModelFormula;
                variablePane.Variables = modelObj.Variables;
                //modelFormulaText.TextChanged += (o, args) => variablePane.Model = modelFormulaText.Text;
                variablePane.HandleFormulaChanged += HandleModelFormulaChanged;
            }
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private List<Answer> _analysis;
        private List<Answer> Analysis
        {
            get
            {
                return _analysis;
            }

            set
            {
                _analysis = value;
                if (_analysis == null)
                {
                    wordDocumentToolStripMenuItem.Enabled = false;
                    variableAnalysisToolStripMenuItem.Enabled = false;
                }
                else
                {
                    wordDocumentToolStripMenuItem.Enabled = true;
                    variableAnalysisToolStripMenuItem.Enabled = true;                    
                }
            }
        }
        private void SaveModelAnalysis(object sender, EventArgs e)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".doc";
            dialog.Filter = Resources.MainForm_SaveModelAnalysis_Doc_File____doc__;

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var fileName = dialog.FileName;

                var form = new PleaseWaitForm();
                Action action = () =>
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Create))
                    {
                        using (var fileWriter = new StreamWriter(fileStream))
                        {
                            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument("Temp");
                            var model = ServiceContainer.ModelService().GetModel(modelFormulaText.Text);

                            fileWriter.WriteLine("<h1>Model Summary</h1>");
                            fileWriter.WriteLine(string.Join(string.Empty,
                                                                ModelAnalyzer.TranslateModel(model, dataset)
                                                                            .Where(kvp => kvp.Key != "Model Intent")
                                                                            .Select(kvp => string.Format("<h2>{0}</h2>", kvp.Key) + kvp.Value)));
                            foreach (var answer in Analysis.Skip(1))
                            {
                                if (answer.Question.GetFormattedQuestion() == "Script Log (raw):") continue;
                                fileWriter.WriteLine("<h1>{0}</h1>", answer.Question.GetFormattedQuestion());
                                fileWriter.WriteLine(answer.GetFormattedAnswer());
                            }

                            fileWriter.Flush();
                        }                            
                    }

                    var d = new CloseFormCallback(form.Close);
                    Invoke(d);
                };

                action.BeginInvoke(action.EndInvoke, null);
                form.ShowDialog();
            }
        }

        private void SaveVariableAnalysis(object sender, EventArgs e)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = Resources.MainForm_SaveVariableAnalysis_Text_File____txt__;

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var fileName = dialog.FileName;

                var form = new PleaseWaitForm();
                Action action = () =>
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Create))
                    {
                        using (var fileWriter = new StreamWriter(fileStream))
                        {
                            fileWriter.Write(
                                string.Join(string.Empty,
                                            Analysis.Where(a => a.Question.GetFormattedQuestion() == "Script Log (raw):")
                                                    .Select(a => a.GetFormattedAnswer())));
                            fileWriter.Flush();
                        }
                    }

                    var d = new CloseFormCallback(form.Close);
                    Invoke(d);
                };

                action.BeginInvoke(action.EndInvoke, null);
                form.ShowDialog();
            }
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            var about = new AboutForm();
            about.ShowDialog();
        }

        private void AnalyzeFormula(object sender, EventArgs e)
        {
            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument("Temp");
            var model = ServiceContainer.ModelService().GetModel(modelFormulaText.Text);
            Analysis = ServiceContainer.AnalyzerService().GetAnswers("Temp", model);

            HandleNewModelSummary(model, dataset);
            AddQuestions(Analysis.Where(ans => ans.Question.QuestionId != QuestionId.DataExplore)
                                 .ToDictionary(ans => ans.Question.GetFormattedQuestion(), ans => ans.GetFormattedAnswer()),
                         true);

        }

        private void ClearRConsole(object sender, EventArgs e)
        {
            rConsole.ClearConsole();
        }
    }
}
