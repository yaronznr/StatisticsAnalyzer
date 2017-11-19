using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    public static class Extensions
    {
        public static IEnumerable<KeyValuePair<T, int>> IndexedSelect<T>(this IEnumerable<T> list)
        {
            var a = 0;
            return list.Select(item => new KeyValuePair<T, int>(item, a++));
        }
    }

    public class ModelValidityQuestion : Question
    {
        private int _currentSubstringIndex;

        private ModelValidityQuestion()
        {
            _currentSubstringIndex = 0;
        }

        private double CookDistanceThreshold(string cookOption, ModelDataset dataset)
        {
            if (cookOption == "4N")
            {
                return 4.0/dataset.DataTable.Rows.Count;
            }

            return 1;
        }

        private string EncodeSubstringIndices(string str)
        {
            var sb = new StringBuilder();

            var stringParts = str.Split(new[] {"{}"}, StringSplitOptions.None);
            sb.Append(stringParts.First());

            foreach (var stringPart in stringParts.Skip(1))
            {
                sb.Append("{");
                sb.Append(_currentSubstringIndex.ToString(CultureInfo.InvariantCulture));
                sb.Append("}");
                _currentSubstringIndex++;
                sb.Append(stringPart);
            }

            return sb.ToString();
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var dataTable = dataset.DataTable;
            var warnings = new List<string>();
            var parameters = new List<string>();

            if (generalMmodelResult.LinearMixedModelResult.CvGlmnetVariables != null)
            {
                if (generalMmodelResult.LinearMixedModelResult.CvGlmnetVariables.Count() != mixedModel.FixedEffectVariables.Count())
                {
                    warnings.Add(
                        "Cross validation suggests model has more predictive power after we ommit several variable. " +
                        "Optimal model is: " +
                        Question.GetChangeModel(mixedModel.PredictedVariable + " ~ " + string.Join(" + ", generalMmodelResult.LinearMixedModelResult.CvGlmnetVariables)));
                }
            }

            // Normality check
            warnings.Add(
                    "We've tested residuals received from the model for normality with Shapiro-Wilk normality test. " +
                    "Test's result was " +
                    StatisticsTextHelper.CreatePValueReport("W",
                                                            modelResult.ResidualStats.ResidualsTest.WValue,
                                                            modelResult.ResidualStats.ResidualsTest.PValue) +
                    ((modelResult.ResidualStats.ResidualsTest.PValue < StatConfigWrapper.MixedConfig.AssumptionTestsConfig.ShapiroWilkTestTestConfig.SigLevel)
                        ? ". Tests suggest that residuals are far from being normal. This means that analysis probably " +
                          "violates normallity assumption. This most is typically rectified by adding another dependant variable " +
                          "or applying some transformation on model's variables (e.g. log, square)."
                        : ". Test shows that residuals are not very far from normality. This suggests model can be considered as a good fit."));

            if (modelResult.ResidualStats.ResidualsTest.PValue < StatConfigWrapper.MixedConfig.AssumptionTestsConfig.ShapiroWilkTestTestConfig.SigLevel)
            {
                var avg = modelResult.ResidualStats.Residuals.Average(x => x*x);
                if (avg.HasValue)
                {
                    var valueCount = modelResult.ResidualStats.Residuals.Where(v => v.HasValue).Count();
                    var threshZ = DistributionHelper.ComputeInverseZ(1.0 / valueCount);
                    var std = Math.Sqrt(avg.Value);
                    var outlierIndices = modelResult.ResidualStats.Residuals.IndexedSelect()
                        .Where(x => x.Key.HasValue &&
                                    (Math.Abs(x.Key.Value/std) > Math.Abs(threshZ) /*StatConfigWrapper.MixedConfig.AssumptionTestsConfig.InfluenceTestsConfig.OutlierVariance * std*/) ||
                                    (modelResult.ModelValidationTest.CookDistanceValues.ObsCookDistances.ContainsKey(x.Value) &&
                                     modelResult.ModelValidationTest.CookDistanceValues.ObsCookDistances[x.Value] >
                                     CookDistanceThreshold(StatConfigWrapper.MixedConfig.AssumptionTestsConfig.InfluenceTestsConfig.CooksDistance, dataset)))
                        .Select(x => x.Value)
                        .ToArray();

                    if (outlierIndices.Any())
                    {
                        var sb = new StringBuilder();

                        sb.Append("These are rows suspected as outliers in the given model:<br>");

                        for (int i = 0; i < outlierIndices.Length; i++)
                        {
                            var row = dataTable.Rows[outlierIndices[i]];
                            var cols = 0;
                            foreach (var variable in mixedModel.AllVariables)
                            {
                                sb.AppendFormat("{0} ", row[variable]);
                                cols++;
                            }
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                if (cols < 10 && !mixedModel.AllVariables.Contains(col.ColumnName))
                                {
                                    sb.AppendFormat("{0} ", row[cols]);
                                    cols++;
                                }
                            }
                            sb.AppendFormat("(SE={0:F2}, Z={1:F2}",
                                            modelResult.ResidualStats.Residuals[outlierIndices[i]],
                                            modelResult.ResidualStats.Residuals[outlierIndices[i]] / std);
                            if (modelResult.ModelValidationTest.CookDistanceValues.ObsCookDistances.ContainsKey(outlierIndices[i]))
                            {
                                sb.AppendFormat(", Cook={0}", modelResult.ModelValidationTest.CookDistanceValues.ObsCookDistances[outlierIndices[i]]);
                            }
                            sb.Append(") ");
                            sb.AppendFormat("<a href=\"javascript:window.mixedModelApp.datacontext.addTransformer('removerow,uniqueid,{0}')\">Remove</a>", row["uniqueid"]);
                            sb.Append("<br>");
                        }
                        warnings.Add(sb.ToString());
                    }
                }
            }

            foreach (var randomEffectVariable in mixedModel.RandomEffectVariables)
            {
                var comparedModelRandomBiasObj = mixedModel.Clone();
                comparedModelRandomBiasObj.RemoveRandomEffectPart(randomEffectVariable);
                var comparedModelRandomBias = modelResult.ModelComparisons.ComparedModels.ContainsKey(randomEffectVariable) ?
                    modelResult.ModelComparisons.ComparedModels[randomEffectVariable] :        // Single random effect case 
                    modelResult.ModelComparisons.ComparedModels[comparedModelRandomBiasObj.ModelFormula]; // Multiple random effect case

                if (comparedModelRandomBias.PValue > StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel)
                {
                    warnings.Add(EncodeSubstringIndices(
                                 "Comparing the given model with a model eliminating random effect {} shows this " +
                                 "element has no significant effect " +
                                 StatisticsTextHelper.CreatePValueReport("X&sup2;", comparedModelRandomBias.ChiDf, comparedModelRandomBias.PValue) +
                                 ". Consider using the simplified model \"{}\""));

                    parameters.Add(randomEffectVariable);
                    parameters.Add(comparedModelRandomBiasObj.ModelFormula);
                }                        

                foreach (var randomEffectRegressor in mixedModel.GetRandomLinearFormula(randomEffectVariable).AllVariables)
                {
                    if (randomEffectRegressor != "1")
                    {
                        var comparedModelObj = mixedModel.Clone();
                        comparedModelObj.RemoveRandomEffectFormulaVariable(randomEffectVariable, randomEffectRegressor);
                        var comparedModel = modelResult.ModelComparisons.ComparedModels[comparedModelObj.ModelFormula];

                        if (comparedModel.PValue > StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel)
                        {
                            warnings.Add(EncodeSubstringIndices("Comparing the given model with a model eliminating effect of {} under random effect {} shows this " +
                                         "element has no significant effect " +
                                         StatisticsTextHelper.CreatePValueReport("X&sup2;", comparedModel.ChiSq, comparedModel.PValue) +
                                         ". Consider using the simplified model \"{}\""));

                            parameters.Add(randomEffectRegressor);
                            parameters.Add(randomEffectVariable);
                            parameters.Add(comparedModelObj.ModelFormula);
                        }                    
                    }
                }
            }

            foreach (var varTest in modelResult.ModelValidationTest.LeveneTests.Keys)
            {
                if (modelResult.ModelValidationTest.LeveneTests[varTest].PValue < StatConfigWrapper.MixedConfig.AssumptionTestsConfig.LeveneTestConfig.SigLevel)
                {
                    warnings.Add(EncodeSubstringIndices(
                                 "Data is very heteroscedastic. When comparing variance of residuals using levene's test for component ({}) we see significant change in variance " +
                                 StatisticsTextHelper.CreatePValueReport("F",
                                                                         modelResult.ModelValidationTest.LeveneTests[varTest].FValue,
                                                                         modelResult.ModelValidationTest.LeveneTests[varTest].PValue)));
                    parameters.Add(varTest.ToString().Trim());
                }
            }

            if (modelResult.ModelValidationTest.BreuschPaganTest != null &&
                modelResult.ModelValidationTest.BreuschPaganTest.PValue < StatConfigWrapper.MixedConfig.AssumptionTestsConfig.BrueshPaganTestConfig.SigLevel)
            {
                warnings.Add("Data is very heteroscedastic. When comparing variance of residuals using Breusch-Pagan test across decimal components we see significant change in variance " +
                             StatisticsTextHelper.CreatePValueReport("X&sup2;",
                                                                     modelResult.ModelValidationTest.BreuschPaganTest.ChiSquare,
                                                                     modelResult.ModelValidationTest.BreuschPaganTest.PValue));
            }

            foreach (var randomEffect in mixedModel.RandomEffectVariables)
            {
                var randomEffectCoef = modelResult.RandomEffectResults[randomEffect].RandomEfects["(Intercept)"];
                foreach (var effect in randomEffectCoef.RandomEffects)
                {
                    if (Math.Abs(effect.Value / randomEffectCoef.StdError) > StatConfigWrapper.MixedConfig.AssumptionTestsConfig.InfluenceTestsConfig.OutlierVariance)
                    {
                        warnings.Add(string.Format("{1} (value of {0}) has an exceedingly large value (Std={2}, RandomBias={3})..",
                                                   randomEffect,
                                                   effect.Key,
                                                   randomEffectCoef.StdError,
                                                   effect.Value));
                    }
                }
            }

            foreach (var randomCookDistance in modelResult.ModelValidationTest.CookDistanceValues.RandomCookDistances)
            {
                foreach (var ff in randomCookDistance.Value
                                                     .Where(v => v.Value > CookDistanceThreshold(StatConfigWrapper.MixedConfig
                                                                                                                  .AssumptionTestsConfig
                                                                                                                  .InfluenceTestsConfig
                                                                                                                  .CooksDistance, 
                                                                                                 dataset)))
                {
                    warnings.Add(string.Format("{1} (value of {0}) has an exceedingly large value (Cook's Distance={2:F2})..",
                                               randomCookDistance.Key,
                                               ff.Key,
                                               ff.Value));
                }
            }

            var graphs = new StringBuilder();
            if (dataset.ModelResult != null)
            {
                graphs.AppendLine(GetChartElement("Q-Q Plot", "placeholder_QQ", "addQQPlot(chartName)", "explainQQ"));
                graphs.AppendLine(GetChartElement("Fitted vs. Residuals", "placeholder_Fitted", "addFittedPlot(chartName)", "explainFittedResid"));
            }

            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = string.Format("<ul><li>{0}</li></ul>{1}",
                                                        string.Join("</li><br><li>", warnings),
                                                        graphs),
                AnswerParameters = parameters,
            };
        }

        public class ModelValidityQuestionFactory : QuestionFactory
        {
            public ModelValidityQuestionFactory() : base(QuestionId.AnalyzeModelValidity)
            {
            }

            public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
            {
                return new List<Question>
                {
                   new ModelValidityQuestion
                   {
                       QuestionId = QuestionId.AnalyzeModelValidity,
                       QuestionInterpertTemplate = "Model Validation Analysis:", 
                       QuestionParameters = new List<string>(),
                   },
                };
            }

            public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
            {
                return dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType != typeof(string);
            }
        }
    }
}
