using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using System;

namespace StatisticsAnalyzerCore.Questions
{
    public class RegressionSummaryQuestion : Question
    {
        private double ComputeZPValue(double z)
        {
            return 1.0 - 2 * Math.Abs(0.5 - z);
        }

        private string CreateTable(List<string> header, List<List<string>> tableData)
        {
            var tb = new StringBuilder();

            tb.AppendLine("<table class='regressionTable'>");
            tb.AppendLine("<tbody>");
            tb.AppendFormat("<tr><th class='regressionTable'>{0}</th></tr>", string.Join("</th><th class='regressionTable'>", header));
            foreach (var tableRow in tableData)
            {
                tb.AppendFormat("<tr><td class='regressionTable'>{0}</td></tr>", string.Join("</td><td class='regressionTable'>", tableRow));
            }
            tb.AppendLine("</tbody>");
            tb.AppendLine("</table><br>");

            return tb.ToString();
        }

        private Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, LinearMixedModelResult modelResult)
        {
            var summary = new StringBuilder();

            summary.AppendLine("<h2>Regression Summary:</h2>");

            if (!mixedModel.RandomEffectVariables.Any())
            {
                summary.Append(CreateTable(new List<string> { "Fixed Effect", "F", "SumSqr", "P.Value" },
                            modelResult.AnovaResult.Select(anovaResult =>
                                                           new List<string>
                                                           {
                                                               anovaResult.Key.ToString(),
                                                               anovaResult.Value.FValue.ToString(CultureInfo.InvariantCulture),
                                                               anovaResult.Value.SumSquare.ToString(CultureInfo.InvariantCulture),
                                                               anovaResult.Value.PValue.ToString(CultureInfo.InvariantCulture),
                                                           })
                                                   .ToList()));
            }
            else
            {
                summary.Append(CreateTable(new List<string> { "Fixed Effect", "F", "P.Value" },
                            modelResult.AnovaResult.Select(anovaResult =>
                                                           new List<string>
                                                           {
                                                               anovaResult.Key.ToString(),
                                                               anovaResult.Value.FValue.ToString(CultureInfo.InvariantCulture),
                                                               anovaResult.Value.PValue.ToString(CultureInfo.InvariantCulture),
                                                           })
                                                   .ToList()));
                summary.Append(CreateTable(new List<string> { "Random Effect", "Std", "X&sup2;", "P.Value" },
                            modelResult.RandomEffectResults
                                       .Select(randomEffect =>
                                       {
                                           var clone = mixedModel.Clone();
                                           clone.RemoveRandomEffectPart(randomEffect.Key);
                                           var compare =
                                               modelResult.ModelComparisons.ComparedModels.ContainsKey(
                                                   clone.ModelFormula)
                                                   ? modelResult.ModelComparisons.ComparedModels[clone.ModelFormula]
                                                   : modelResult.ModelComparisons.ComparedModels[randomEffect.Key];
                                           return new List<string>
                                           {
                                               randomEffect.Key,
                                               randomEffect.Value.RandomEfects[("(Intercept)")].Variance.ToString(CultureInfo.InvariantCulture),
                                               compare.ChiSq.ToString(CultureInfo.InvariantCulture),
                                               compare.PValue.ToString(CultureInfo.InvariantCulture),
                                           };
                                       })
                                       .ToList()));
            }

            if (modelResult.ResidualStats.Residuals.Any(x => !x.IsNull()))
            {
                // ReSharper disable PossibleInvalidOperationException
                var residSquare = modelResult.ResidualStats.Residuals.Average(x => x * x).Value;
                var residMean = modelResult.ResidualStats.Residuals.Average().Value;
                // ReSharper restore PossibleInvalidOperationException
                summary.Append(CreateTable(new List<string> { "Std(Error)", "Min", "Max", "Normality" },
                    new List<List<string>> { new List<string>
                    {
                        (residSquare - (residMean*residMean)).ToString(CultureInfo.InvariantCulture),
                        modelResult.ResidualStats.Min.ToString(CultureInfo.InvariantCulture),
                        modelResult.ResidualStats.Max.ToString(CultureInfo.InvariantCulture),
                        modelResult.ResidualStats.ResidualsTest.PValue.ToString(CultureInfo.InvariantCulture),
                    }}));
            }
            var pvalueTitle = dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType == typeof(string) ? "P(Z>0)" : "P(T>0)";
            summary.Append(CreateTable(new List<string> { "Fixed Effect", "Level", "Estimate", "Confidence Interval (95%)", pvalueTitle },
                                       modelResult.FixedEffectResults
                                                  .SelectMany(fe =>
                                                              fe.Value
                                                                .EffectResults
                                                                .Select(e => new List<string>
                                                                {
                                                                    fe.Key.ToString(),
                                                                    e.Key.ToString(),
                                                                    e.Value.Estimate.ToString(CultureInfo.InvariantCulture),
                                                                    "Not computed",
                                                                    mixedModel.RandomEffectVariables.Any() ?
                                                                        (1-DistributionHelper.ComputeZ(e.Value.TValue)).ToString("F5", CultureInfo.InvariantCulture) :
                                                                        e.Value.PValue.ToString(CultureInfo.InvariantCulture)
                                                                }))
                                                  .ToList()));

            return new HtmlAnswer
            {
                AnswerInterpertTemplate = summary.ToString(),
                AnswerParameters = new List<string>(),
                Question = this,
            };
        }

        private Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, BinomialMixedModelResult modelResult)
        {
            var summary = new StringBuilder();
            summary.AppendLine("<h2>Regression Summary:</h2>");
            summary.Append(CreateTable(new List<string> { "AIC", "BIC", "Log Likelihood", "Deviance" },
                                       new List<List<string>> { 
                                           new List<string> { 
                                               modelResult.ModelFitResult.Aic.ToString(CultureInfo.InvariantCulture),
                                               modelResult.ModelFitResult.Bic.ToString(CultureInfo.InvariantCulture),
                                               modelResult.ModelFitResult.LogLikelihood.ToString(CultureInfo.InvariantCulture),
                                               modelResult.ModelFitResult.Deviance.ToString(CultureInfo.InvariantCulture),
                                           }
                                       }));
            if (mixedModel.RandomEffectVariables.Any())
            {
                summary.Append(CreateTable(new List<string> { "Random Effect", "Std", "X&sup2;", "P.Value" },
                            modelResult.RandomEffectResults
                                       .Select(randomEffect =>
                                       {
                                           var clone = mixedModel.Clone();
                                           clone.RemoveRandomEffectPart(randomEffect.Key);
                                           var compare =
                                               modelResult.ModelComparisons.ComparedModels.ContainsKey(
                                                   clone.ModelFormula)
                                                   ? modelResult.ModelComparisons.ComparedModels[clone.ModelFormula]
                                                   : modelResult.ModelComparisons.ComparedModels[randomEffect.Key];
                                           return new List<string>
                                           {
                                               randomEffect.Key,
                                               randomEffect.Value.RandomEfects[("(Intercept)")].Variance.ToString(CultureInfo.InvariantCulture),
                                               compare.ChiSq.ToString(CultureInfo.InvariantCulture),
                                               compare.PValue.ToString(CultureInfo.InvariantCulture),
                                           };
                                       })
                                       .ToList()));
            }
            var pvalueTitle = dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType == typeof(string) ? "P(Z>0)" : "P(T>0)";
            summary.Append(CreateTable(new List<string> { "Fixed Effect", "Level", "Estimate", "Confidence Interval (95%)", pvalueTitle },
                                       modelResult.FixedEffectResults
                                                  .SelectMany(fe =>
                                                              fe.Value
                                                                .EffectResults
                                                                .Select(e => new List<string>
                                                                {
                                                                    fe.Key.ToString(),
                                                                    e.Key.ToString(),
                                                                    e.Value.Estimate.ToString(CultureInfo.InvariantCulture),
                                                                    "Not computed",
                                                                    mixedModel.RandomEffectVariables.Any() ?
                                                                        (ComputeZPValue(DistributionHelper.ComputeZ(e.Value.TValue))).ToString("F5", CultureInfo.InvariantCulture) :
                                                                        e.Value.PValue.ToString(CultureInfo.InvariantCulture)
                                                                }))
                                                  .ToList()));

            return new HtmlAnswer
            {
                AnswerInterpertTemplate = summary.ToString(),
                AnswerParameters = new List<string>(),
                Question = this,
            };
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalModelResult)
        {
            if (generalModelResult.LinearMixedModelResult != null)
            {
                return AnalyzeAnswer(dataset, mixedModel, generalModelResult.LinearMixedModelResult);
            }
            
            return AnalyzeAnswer(dataset, mixedModel, generalModelResult.BinomialMixedModelResult);
        }
    }

    public class RegressionSummaryQuestionFactory : QuestionFactory
    {
        public RegressionSummaryQuestionFactory() : base(QuestionId.ModelSummary)
        {    
        }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return new List<Question> 
            { 
                new RegressionSummaryQuestion
                {
                    QuestionId = QuestionId.ModelSummary,
                    QuestionInterpertTemplate = "Regression Summary:",
                    QuestionParameters = new List<string>(),
                }, 
            };
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return true;
        }
    }
}
