using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StatisticsAnalyzerCore;
using NaturalLanguageLib;
using AzureCore;
using ExcelLib;
using RLib;
using System.IO;

namespace WebUI
{
    public partial class MainPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AnalzeModel(false);
        }

        protected void AnalyzeButton_Click(object sender, EventArgs e)
        {
            AnalzeModel(true);
        }

        private void AnalzeModel(bool runModel)
        {
            try
            {
                ModelAnswer.Text = string.Empty;

                Guid userId = UserManager.GetUserId(Request, Response);
                var stream = AzureHelper.DownloadBlob(
                    AzureHelper.DefaultStorageAccount,
                    AzureHelper.DefaultStorageKey,
                    "files",
                    string.Format("{0}/blob.xlsx", userId.ToString()));

                var excelFile = new ExcelDocument(stream);
                var dataTable = excelFile.LoadCellData();

                MixedLinearModel model;
                if (string.IsNullOrEmpty(FormulaText.Text))
                {
                    model = ModelGenerator.PerdictModel(dataTable);
                }
                else
                {
                    model = new MixedLinearModel(FormulaText.Text);
                }

                FormulaText.Text = model.ModelFormula;
                InterpertModel.Text = ModelTranslator.TranslateModel(model, dataTable);

                //ModelTranslator.GenerateSuggestedQuestions(model, dataTable);

                if (runModel)
                {
                    try
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);
                        var scriptResponse = RHelper.RunRemoteScript(RScriptFactory.CreateMixedModelScript(model), bytes);
                        AnalyzedModel.Text = scriptResponse.First(cmd => cmd.Contains("summary(model)"))
                                                           .Replace("\n", "<br>")
                                                           .Replace(" ", "&nbsp;");

                        Anova.Text = scriptResponse.First(cmd => cmd.Contains("anova(model)"))
                                                   .Replace("\n", "<br>")
                                                   .Replace(" ", "&nbsp;");
                                                   
                        var result = RMixedModelResultParser.ParseMixedModelResult(model,
                                                                                   dataTable,
                                                                                   AnalyzedModel.Text,
                                                                                   string.Empty);

                        if (model.RandomEffectVariables.Count() == 0 &&
                            model.FixedEffectVariables.Count() == 1)
                        {
                            var columnName = model.FixedEffectVariables.Single();
                            var tableStats = TableManipulations.GetTableStats(dataTable);
                            if (tableStats.ColumnStats[columnName].ValuesCount.Count() == 2 &&
                                dataTable.Columns[columnName].DataType == typeof(string))
                            {
                                var effectResult = (FixedLevelEffectResult)result.FixedEffectResults[columnName];
                                var response = effectResult.EffectResults.First().Value;

                                if (Math.Abs(response.TValue) >= 3.0)
                                {
                                    ModelAnswer.Text = string.Format(
                                        "Values for {0} and {1} are significantely different. Averages between the two " +
                                        "are {2}. After running a student's T-Test we found T-Value of {3}",
                                        tableStats.ColumnStats[model.FixedEffectVariables.First()].ValuesCount.Keys.ToArray()[0],
                                        tableStats.ColumnStats[model.FixedEffectVariables.First()].ValuesCount.Keys.ToArray()[1],
                                        response.Estimate,
                                        response.TValue);
                                }
                                else
                                {
                                    ModelAnswer.Text = string.Format(
                                        "Values for {0} and {1} are not significantely different." +
                                        " After running a student's T-Test we found T-Value of {2}",
                                        tableStats.ColumnStats[model.FixedEffectVariables.First()].ValuesCount.Keys.ToArray()[0],
                                        tableStats.ColumnStats[model.FixedEffectVariables.First()].ValuesCount.Keys.ToArray()[1],
                                        response.TValue);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //AnalyzedModel.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                InterpertModel.Text = "Error loading Excel files" + ex;
            }
        }
    }
}
