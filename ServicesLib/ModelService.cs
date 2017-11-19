using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.Questions;

namespace ServicesLib
{
    public class ModelService
    {
        private ModelAnalysis GetCachedAnalysis(ModelDataset dataset,
                                                MixedLinearModel mixedModel,
                                                string fileName,
                                                string userName)
        {
            IList<string> rawResult = null;
            if (dataset.ModelResult != null &&
                dataset.ModelResult.RawResult != null)
            {
                rawResult = dataset.ModelResult.RawResult;
            }
            else
            {
                var rawResultStr = ServiceContainer.StorageService().GetModelAnalysis(userName, fileName);
                if (rawResultStr != null)
                {
                    rawResult = rawResultStr.Split(new[] { "> " }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (rawResult != null)
            {
                if (mixedModel.HasIterator)
                {
                    var d = ServiceContainer.AnalyzerService().PerformMultipleAnalysis(rawResult[0]);
                    return new ModelAnalysis
                    {
                        Questions = new List<QuestionAnalysis>
                        {
                            new QuestionAnalysis
                            {
                                Question = "Variable Analysis:",
                                Answer = new MultiTestAnalysisQuestion().AnalyzeAnswer(d).GetFormattedAnswer(),
                            },
                        },
                    };
                }

                var answers = ServiceContainer.AnalyzerService().GetAnswers(userName,
                                                            mixedModel,
                                                            dataset,
                                                            rawResult.ToList());
                return new ModelAnalysis
                {
                    AnovaAnalysis = answers.Count > 2 ?
                                        answers[1].GetFormattedAnswer().Replace("\n", "<br>").Replace(" ", "&nbsp;") :
                                        string.Empty,
                    RegressionAnalysis = answers.Count > 2 ?
                                            answers[0].GetFormattedAnswer().Replace("\n", "<br>").Replace(" ", "&nbsp;") :
                                            string.Empty,
                    Questions = answers.Select(
                        ans =>
                        new QuestionAnalysis
                        {
                            Question = ans.Question.GetFormattedQuestion(),
                            Answer = ans.GetFormattedAnswer(),
                        }).ToList(),
                };
            }

            return null;
        }

        public MixedLinearModel GetModel(string formula, ModelDataset dataset, string userName, string fileName)
        {
            if (string.IsNullOrEmpty(formula))
            {
                var modelCount = ServiceContainer.StorageService().GetModelCount(userName, fileName);
                return modelCount > 0 ? 
                    new MixedLinearModel(ServiceContainer.StorageService().GetModelFormula(userName, fileName, modelCount)) : 
                    ModelGenerator.PerdictModel(dataset);
            }

            return new MixedLinearModel(formula);
        }

        public MixedLinearModel GetModel(string formula)
        {
            return new MixedLinearModel(formula);
        }

        public ModelAnalysis GetModelAnalysis(string userName, string formula)
        {
            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument(userName);

            var fileName = ServiceContainer.StorageService().GetCurrentExcelName(userName);
            if (!string.IsNullOrEmpty(formula))
            {
                ServiceContainer.StorageService().AddModelFormula(userName, fileName, formula);
                dataset.ModelResult = null;
            }

            var mixedModel = ServiceContainer.ModelService().GetModel(formula);

            // Perform analysis
            if (mixedModel.HasIterator)
            {
                var d = ServiceContainer.AnalyzerService().PerformMultipleAnalysis(userName, formula);
                return new ModelAnalysis
                {
                    Questions = new List<QuestionAnalysis>
                    {
                        new QuestionAnalysis
                        {
                            Question = "Variable Analysis:",
                            Answer = new MultiTestAnalysisQuestion().AnalyzeAnswer(d).GetFormattedAnswer(),
                        },
                    },
                };
            }

            var answers = ServiceContainer.AnalyzerService().GetAnswers(userName, mixedModel);

            // Cache analysis result
            if (dataset.ModelResult != null)
            {
                ServiceContainer.StorageService().SetModelAnalysis(userName,
                                                                   fileName,
                                                                   string.Join("> ", dataset.ModelResult.RawResult));

            }

            return new ModelAnalysis
            {
                AnovaAnalysis = answers.Count > 2 ?
                                    answers[1].GetFormattedAnswer().Replace("\n", "<br>").Replace(" ", "&nbsp;") :
                                    string.Empty,
                RegressionAnalysis = answers.Count > 2 ?
                                        answers[0].GetFormattedAnswer().Replace("\n", "<br>").Replace(" ", "&nbsp;") :
                                        string.Empty,
                Questions = answers.Where(a => a.Question.QuestionId != QuestionId.DataExplore)
                                   .Select(ans => new QuestionAnalysis
                                                  {
                                                      Question = ans.Question.GetFormattedQuestion(),
                                                      Answer = ans.GetFormattedAnswer(),
                                                  })
                                   .ToList(),
            };

        }

        public MixedModel GetModel(string formula, string userName)
        {
            var dataTable = ServiceContainer.ExcelDocumentService().GetExcelDocument(userName);
            if (dataTable != null)
            {
                dataTable.ModelResult = null; // Purge old model result

                var fileName = ServiceContainer.StorageService().GetCurrentExcelName(userName);
                var mixedModel = ServiceContainer.ModelService().GetModel(formula, dataTable, userName, fileName);
                ServiceContainer.StorageService().AddModelFormula(userName, fileName, mixedModel.ModelFormula);

                var variables = new List<ModelVariable>();
                foreach (DataColumn col in dataTable.DataTable.Columns)
                {
                    variables.Add(new ModelVariable
                    {
                        ModelVariableId = 1,
                        Name = col.ColumnName,
                        Type = ModelVariable.ToFieldType(col.DataType),
                        ValueCount = dataTable.TableStats.ColumnStats[col.ColumnName].ValuesCount.Count,
                        Average = dataTable.TableStats.ColumnStats[col.ColumnName].ValuesAverage,
                        Std = dataTable.TableStats.ColumnStats[col.ColumnName].ValuesStd,
                    });
                }

                string dataPage = string.Empty;
                var translatedModel = new Dictionary<string, string>();
                var modelIntent = string.Empty;
                var formulaString = mixedModel.ModelFormula;
                try
                {
                    dataPage = mixedModel.HasIterator
                        ? string.Empty
                        : new DataExploreQuestion().AnalyzeAnswer(dataTable, mixedModel, null).AnswerInterpertTemplate;
                    translatedModel = ModelAnalyzer.TranslateModel(mixedModel, dataTable);
                    modelIntent = translatedModel.Single(kvp => kvp.Key == "Model Intent").Value;
                }
                catch (Exception) // Slip data page when analysis fails
                {
                    formulaString = ModelGenerator.PerdictModel(dataTable).ModelFormula;
                }

                return new MixedModel
                {
                    ModelId = 1,
                    Formula = formulaString,
                    FileName = fileName,
                    ModelInterpert = string.Join(string.Empty,
                                                 translatedModel.Where(kvp => kvp.Key != "Model Intent")
                                                                .Select(kvp => string.Format("<h2>{0}</h2>", kvp.Key) + kvp.Value)) +
                                     "<h2>Suggested Questions</h2>" + string.Join("<br>",
                                         ServiceContainer.AnalyzerService()
                                             .GetQuestions(mixedModel, dataTable)
                                             .Select(q => q.GetFormattedQuestion())
                                             .Except(new[] { "The Data:", 
                                                             "Model Validation Analysis:",
                                                             "Regression Summary:"
                                             })
                                             .Select(q => q + "?")),
                    ModelIntent = modelIntent,
                    Data = dataPage,
                    Variables = variables.Where(v => v.Name != "uniqueid").Take(500).ToList(),
                    RowCount = dataTable.DataTable.Rows.Count,
                    TableAnalysis = dataTable.TableStats.TableAnalysis,
                    ModelAnalysis = GetCachedAnalysis(dataTable, mixedModel, fileName, userName),
                };
            }

            return new MixedModel
            {
                ModelId = 1,
                Formula = "",
                ModelInterpert = "",
                Variables = new List<ModelVariable>(),
            };            
        }
    }
}
