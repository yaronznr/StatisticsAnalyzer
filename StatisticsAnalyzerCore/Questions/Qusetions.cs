using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public enum QuestionId
    {
        ModelSummary,
        ModelAnova,
        DataExplore,
        DoXandYHaveDifferentZ,
        HowXInfluenceZ,
        HowVariablesInfluenceZ,
        DoesXVaryForDifferentY,
        AnalyzeModelValidity,
        ConvergenceIssues,
    }

    public abstract class Question
    {
        protected readonly List<string> HtmlElements = new List<string>();

        public QuestionId QuestionId { get; set; }
        public string QuestionInterpertTemplate { get; set; }
        public List<string> QuestionParameters { protected get; set; }

        public string GetFormattedQuestion()
        {
            return string.Format(QuestionInterpertTemplate, QuestionParameters.Cast<Object>().ToArray());
        }

        protected List<IEnumerable<string>> GetCharts(MixedLinearModel mixedModel)
        {
            // Get fixed effects interactions
            var charts = mixedModel.GetFixedLinearFormula()
                .VariableGroups
                .Where(grp =>
                {
                    var values = grp as string[] ?? grp.ToArray();
                    return values.Length <= 2;
                })
                .ToList();

            // Get interaction between random groups and their covariates
            charts.AddRange(
                mixedModel.RandomEffectVariables
                          .Where(rv => mixedModel.GetRandomLinearFormula(rv).AllVariables.Count() >= 2)
                          .SelectMany(rv => mixedModel.GetRandomLinearFormula(rv)
                                                      .AllVariables
                                                      .Except(new[] { "0", "1" })
                                                      .Select(cv => new[] { rv, cv })));


            return charts;
        }

        protected void AddTitle(string title)
        {
            HtmlElements.Add(string.Format("<h2>{0}</h2>", title));
        }

        public static string GetChangeModel(string newModel)
        {
            return 
                string.Format("<a href=\"javascript:window.mixedModelApp.modelViewModel.setNewModel('{0}')\">{0}</a>",
                              newModel);
        }

        protected string[] GetGroupJsChartFunction(string predictedValue, string[] values, DataTable dataTable)
        {
            var jsFunction = string.Empty;
            if (dataTable.Columns[predictedValue].DataType != typeof(string))
            {
                // No sub-grouping
                if (values.Length == 1)
                {
                    jsFunction = dataTable.Columns[values[0]].DataType == typeof(string) ? "addBarChart" : "addLineChart";
                }
                else
                {
                    // Single sub-grouping
                    jsFunction = dataTable.Columns[values[0]].DataType == typeof(string) ? "addTwoCategoryBarChart" : "addMultipleLineChart";
                }
                return new[]
                {
                    jsFunction,
                    "'Average','Stddev'",
                    string.Join(",", values),
                    string.Format("{0} vs. {1}", predictedValue, string.Join(",", values)),
                };
            }
            else
            {
                // No sub-grouping
                return dataTable.Columns[values[0]].DataType == typeof(string) ?
                    new[]
                    {
                        "addTwoCategoryBarChart",
                        "'Count', 'None'",
                        string.Join(",", values.Concat(new[] { predictedValue })),
                        string.Format("Row# vs. {0} and {1}", predictedValue, string.Join(",", values)),
                    } :
                    new[]
                    {
                        "addLowessChart",
                        "'Value', 'Lowess'",
                        string.Join(",", values),
                        string.Format("{0} occurrences vs. {1}", predictedValue, string.Join(",", values)),
                    };
            }
        }

        protected void AddChartElement(string chartTitle,
                                       string chartName,
                                       string chartJsCall)
        {
            HtmlElements.Add(GetChartElement(chartTitle, chartName, chartJsCall, null));
        }

        protected string GetChartElement(IEnumerable<string> chart, DataTable dataTable, MixedLinearModel mixedModel)
        {
            var enumerable = chart as string[] ?? chart.ToArray();
            var values = enumerable.Where(e => dataTable.Columns[e].DataType != typeof(string))
                            .Concat(enumerable.Where(e => dataTable.Columns[e].DataType == typeof(string)))
                            .ToArray();
            var jsFunction = GetGroupJsChartFunction(mixedModel.PredictedVariable, values, dataTable);
            return GetChartElement(
                jsFunction[3],
                string.Format("placeholder_{0}", string.Join("_", values.Select(v => v.Replace(".", string.Empty)))),
                string.Format("{0}({1}, '{2}:{3}', chartName)",
                              jsFunction[0],
                              jsFunction[1],
                              jsFunction[2],
                              mixedModel.PredictedVariable),
                null);
        }

        protected void AddChartElement(IEnumerable<string> chart, DataTable dataTable, MixedLinearModel mixedModel)
        {
            HtmlElements.Add(GetChartElement(chart, dataTable, mixedModel));
        }

        protected string GetChartElement(string chartTitle,
                                       string chartName,
                                       string chartJsCall,
                                       string explainMeythod
            )
        {
            return string.Format(
                string.Join(
                    Environment.NewLine,
                    "<div class='dataViz'>{0}</div>",
                    "<div id='{1}' class='chartPlaceholder'><div class='chartWaiting'>Loading...</div></div><div class='chartAuxilary scrollable' id='{1}-aux'></div>",
                    "<script type=\"text/javascript\">var chartName='{1}';{2};</script>",
                    string.IsNullOrEmpty(explainMeythod) ? "" : string.Format("<a href=\"javascript:window.mixedModelApp.datacontext.{0}()\">How to interpert this chart?</a>", explainMeythod)),
                chartTitle,
                chartName,
                chartJsCall);
        }

        public abstract Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult);
    }

    public class Answer
    {
        public Question Question { get; set; }
        public List<string> AnswerParameters;
        public string AnswerInterpertTemplate { get; set; }

        public virtual string GetFormattedAnswer() { return string.Format(AnswerInterpertTemplate, AnswerParameters.Cast<Object>().ToArray()); }
    }

    public class HtmlAnswer : Answer
    {
        public override string GetFormattedAnswer() { return AnswerInterpertTemplate; }
    }

    public abstract class QuestionFactory
    {
        public QuestionId QuestionId { get; private set; }

        protected QuestionFactory(QuestionId questionId)
        {
            QuestionId = questionId;
        }

        protected QuestionFactory()
        {
            throw new NotImplementedException();
        }

        public abstract List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel);
        public abstract bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel);
        
        public bool TryCreateQuestion(ModelDataset dataset, MixedLinearModel mixedModel, out List<Question> questions)
        {
            if (!IsQuestionApplicable(dataset, mixedModel))
            {
                questions = null;
                return false;
            }

            questions = CreateQuestions(dataset, mixedModel);
            return true;
        }
    }

    public class SimpleQuestion : Question
    {
        private string answer;

        public SimpleQuestion(string questiontext, string answerText, QuestionId questionId)
        {
            QuestionId = questionId;
            QuestionInterpertTemplate = questiontext;
            QuestionParameters = new List<string>();

            answer = answerText;
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult modelResult)
        {
            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = answer,
                AnswerParameters = new List<string>(),
            };
        }
    }
}
