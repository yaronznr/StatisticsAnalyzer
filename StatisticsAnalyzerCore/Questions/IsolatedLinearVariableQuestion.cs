using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    public class LinearVariableTwoValuesQuestion : Question
    {
        public string PredictingVariable { get; set; }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var columnName = PredictingVariable;
            var effectResult = modelResult != null ?
                modelResult.FixedEffectResults[new VarGroupIndex(columnName)] :
                generalMmodelResult.BinomialMixedModelResult.FixedEffectResults[new VarGroupIndex(columnName)];
            var response = effectResult.EffectResults.First().Value;
            var responseVariable = effectResult.EffectResults.First().Key.ValueNames[0];

            // Get fixed effects interactions
            var charts = GetCharts(mixedModel).First(c => c.Contains(columnName));

            // Binomial models always uses z-score
            if (modelResult == null)
            {
                int index = dataset.DataTable.Rows[0].Table.Columns.IndexOf(mixedModel.PredictedVariable);
                var zeroLevelValue = dataset.DataTable.Rows[0][index] as string;
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                              ((Math.Abs(response.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "occurrence probabilities for {1} and {2} are significantly different. " +
                                                    StatisticsTextHelper.CreateBinomialLargerThanStatement(
                                                        mixedModel.PredictedVariable,
                                                        zeroLevelValue,
                                                        columnName,
                                                        responseVariable,
                                                        QuestionParameters[0],
                                                        QuestionParameters[1],
                                                        response.Estimate) +
                                                    "After analyzing Z-Score we found significant results "
                                                    :
                                                    "occurrence probabilities  for {1} and {2} are not significantly different. Average difference between " +
                                                    "the two is {3}. After analyzing Z-Score we did not found significant results ") +
                                              StatisticsTextHelper.CreatePValueReport("Z", response.TValue, response.PValue) +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                        {
                            string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                            QuestionParameters[0],
                            QuestionParameters[1],
                            Math.Abs(response.Estimate).ToString(CultureInfo.InvariantCulture),
                            response.TValue.ToString(CultureInfo.InvariantCulture),
                            response.PValue.ToString(CultureInfo.InvariantCulture),
                        }
                };
            }

            if (!mixedModel.RandomEffectVariables.Any())
            {
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                              ((Math.Abs(response.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "values for {1} and {2} are significantly different. " +
                                                    StatisticsTextHelper.CreateLargerThanStatement(responseVariable,
                                                                                                   QuestionParameters[0],
                                                                                                   QuestionParameters[1],
                                                                                                   response.Estimate) +
                                                    "After running a student's T-Test we found significant results "
                                                    :
                                                    "values for {1} and {2} are not significantly different. Average difference between " +
                                                    "the two is {3}. After running a student's T-Test we did not found significant results ") +
                                              StatisticsTextHelper.CreatePValueReport("T", response.TValue, response.PValue) +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                AnswerParameters = new List<string>
                        {
                            string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                            QuestionParameters[0],
                            QuestionParameters[1],
                            Math.Abs(response.Estimate).ToString(CultureInfo.InvariantCulture),
                            response.TValue.ToString(CultureInfo.InvariantCulture),
                            response.PValue.ToString(CultureInfo.InvariantCulture),
                        }
                };
            }
            
            var krAnovaResult = modelResult.AnovaResult[new VarGroupIndex(columnName)];
            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                            ((krAnovaResult.PValue <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                "Values for {1} and {2} are significantly different. " +
                                                StatisticsTextHelper.CreateLargerThanStatement(responseVariable,
                                                                                               QuestionParameters[0],
                                                                                               QuestionParameters[1],
                                                                                               response.Estimate) +
                                                "After running a {4}F-Test with Kenward-Rogers DF we found significant results "
                                                :
                                                "Values for {1} and {2} are not significantly different. Average difference between " +
                                                "the two is {3}. After running a {4}F-Test with Kenward-Rogers DF we did not found significant results ") +
                                            StatisticsTextHelper.CreatePValueReport("F", krAnovaResult.FValue, krAnovaResult.PValue) +
                                            GetChartElement(charts, dataset.DataTable, mixedModel),
                AnswerParameters = new List<string>
                    {
                        string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                        QuestionParameters[0],
                        QuestionParameters[1],
                        Math.Abs(response.Estimate).ToString(CultureInfo.InvariantCulture),
                        AnovaNamingHelper.GetAnovaName(dataset, new List<string> { columnName }, true)
                    }
            };
        }
    }

    public class LinearVariableMultipleValuesQuestion : Question
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalModelResult)
        {
            var modelResult = generalModelResult.LinearMixedModelResult;

            var dataTable = dataset.DataTable;
            var columnName = QuestionParameters[0];
            var charts = GetCharts(mixedModel).First(c => c.Contains(columnName));

            var continousEffect = modelResult != null ?
                modelResult.FixedEffectResults[new VarGroupIndex(columnName)] :
                generalModelResult.BinomialMixedModelResult.FixedEffectResults[new VarGroupIndex(columnName)];

            if (continousEffect != null) 
            {
                var effect = continousEffect.EffectResults.First().Value;
                
                // Binomial regression case:
                // - we always use simple z-value
                // - since we don't have ANOVA this is also
                //   relevant to the case of multiple values
                if (modelResult == null)
                {
                    int index = dataset.DataTable.Rows[0].Table.Columns.IndexOf(mixedModel.PredictedVariable);
                    var zeroLevelValue = dataset.DataTable.Rows[0][index] as string;

                    return new Answer
                    {
                        Question = this,
                        AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                                  ((Math.Abs(effect.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                        "{1} has a significant connection to {2}. " +
                                                        StatisticsTextHelper.CreateBinomialSlopeStatement(mixedModel.PredictedVariable,
                                                                                                          zeroLevelValue,
                                                                                                          columnName,
                                                                                                          effect.Estimate) +
                                                        "Analyzing the Z-scores we found significant results, for example:" +
                                                        StatisticsTextHelper.CreatePValueReport("Z", effect.TValue, effect.PValue)
                                                        :
                                                        "{1} does not have a significant connection to {2}. We estimated a slope of {3} " +
                                                        "In the given model. However, no p-value is signifincant" +
                                                        ", we fail to reject the zero effect hypothesis.") +
                                                  GetChartElement(charts, dataset.DataTable, mixedModel),
                        AnswerParameters = new List<string>
                            {
                                string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                                columnName,
                                mixedModel.PredictedVariable,
                                effect.Estimate.ToString(CultureInfo.InvariantCulture),
                                effect.TValue.ToString(CultureInfo.InvariantCulture),
                                effect.PValue.ToString(CultureInfo.InvariantCulture),
                            }
                    };
                }

                if (!mixedModel.RandomEffectVariables.Any() && dataTable.Columns[columnName].DataType != typeof(string))
                {
                    return new Answer
                    {
                        Question = this,
                        AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                                  ((Math.Abs(effect.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                        "{1} has a significant linear connection with {2}. " +
                                                        StatisticsTextHelper.CreateSlopeStatement(mixedModel.PredictedVariable, columnName, effect.Estimate) +
                                                        "After running a student's T-Test we found significant results " +
                                                        StatisticsTextHelper.CreatePValueReport("T", effect.TValue, effect.PValue)
                                                        :
                                                        "{1} does not have a significant linear connection with {2}. We estimated a slope of {3} " +
                                                        "In the given model. However, due to results " +
                                                        StatisticsTextHelper.CreatePValueReport("T", effect.TValue, effect.PValue) +
                                                        ", we fail to reject the zero slope hypothesis.") +
                                                  GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                            {
                                string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                                columnName,
                                mixedModel.PredictedVariable,
                                effect.Estimate.ToString(CultureInfo.InvariantCulture),
                                effect.TValue.ToString(CultureInfo.InvariantCulture),
                                effect.PValue.ToString(CultureInfo.InvariantCulture),
                            }
                    };
                }
            
                var krAnovaResult = modelResult.AnovaResult[new VarGroupIndex(columnName)];
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                                ((krAnovaResult.PValue <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "{1} has a significant linear connection with {2}. " +
                                                    StatisticsTextHelper.CreateSlopeStatement(mixedModel.PredictedVariable, columnName, effect.Estimate) +
                                                    "After running a {4}F-Test with Kenward-Rogers DF we found " +
                                                    StatisticsTextHelper.CreatePValueReport("F", krAnovaResult.FValue, krAnovaResult.PValue)
                                                    :
                                                    "{1} does not have a significant linear connection with {2}. Estimated slope in given model " +
                                                    "is {3}. However, due to the low Kenward-Rogers {4}F-Test " +
                                                    StatisticsTextHelper.CreatePValueReport("F", krAnovaResult.FValue, krAnovaResult.PValue) +
                                                    " we fail to reject the zero slope hypothesis") +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                        {
                            string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                            QuestionParameters[0],
                            QuestionParameters[1],
                            effect.Estimate.ToString(CultureInfo.InvariantCulture),
                            AnovaNamingHelper.GetAnovaName(dataset, new List<string> { columnName }, true)
                        }
                };
            }
            
            AnovaResult anovaResult;
            if (modelResult.AnovaResult.TryGetValue(new VarGroupIndex(columnName), out anovaResult))
            {
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "After controlling the influence of other models' variables ({0}), we can conclude that " +
                                              ((Math.Abs(anovaResult.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "{1} has a significant effect on {2}. Measuring the {5}F-Value in the model we found significant results "
                                                    :
                                                    "{1} does not have a significant effect on {2}. Measuring the {5}F-Value in the model we did not found significant results ") +
                                              StatisticsTextHelper.CreatePValueReport("F", anovaResult.FValue, anovaResult.PValue) +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                            {
                                string.Join(",", MixedModelHelper.GetAllAffectingVariablesExceptOne(mixedModel, columnName)),
                                QuestionParameters[0],
                                QuestionParameters[1],
                                anovaResult.FValue.ToString(CultureInfo.InvariantCulture),
                                anovaResult.PValue.ToString(CultureInfo.InvariantCulture),
                                mixedModel.RandomEffectVariables.Any() ? "Kenward-Rogers " : string.Empty,
                            }
                };
            }
            
            throw new MixedModelException("Unexpected effect level..");
        }
    }

    public class IsolatedLinearVariableQuestionFactory : QuestionFactory
    {
        public IsolatedLinearVariableQuestionFactory() : base(QuestionId.HowXInfluenceZ) { }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var questions = new List<Question>();
            var tableStats = dataset.TableStats;

            foreach (var fixedVarGroup in mixedModel.GetFixedLinearFormula().VariableGroups.Where(grp => grp.Count() == 1))
            {
                var fixedVar = fixedVarGroup.Single();

                if (tableStats.ColumnStats[fixedVar].ValuesCount.Count() > 2)
                {
                    questions.Add(new LinearVariableMultipleValuesQuestion
                    {
                        QuestionId = QuestionId,
                        QuestionInterpertTemplate = "How does {0} influence {1}",
                        QuestionParameters = new List<string> { 
                            fixedVar,
                            mixedModel.PredictedVariable,
                        }
                    });
                }
                else if (tableStats.ColumnStats[fixedVar].ValuesCount.Count() == 2)
                {
                    questions.Add(new LinearVariableTwoValuesQuestion
                    {
                        PredictingVariable = fixedVar,
                        QuestionId = QuestionId,
                        QuestionInterpertTemplate = "Do {0} and {1} have different values for {2}",
                        QuestionParameters = new List<string> { 
                            tableStats.ColumnStats[fixedVar].ValuesCount.Keys.ElementAt(0).ToString(), 
                            tableStats.ColumnStats[fixedVar].ValuesCount.Keys.ElementAt(1).ToString(),
                            mixedModel.PredictedVariable,
                        },
                    });
                }
            }

            return questions;
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var fixedVarCount = mixedModel.FixedEffectVariables.Count();
            var totalvarCount = mixedModel.FixedEffectVariables.Count() + mixedModel.RandomEffectVariables.Count();

            if (totalvarCount > 1 &&
                fixedVarCount >= 1 &&
                mixedModel.GetFixedLinearFormula().VariableGroups.Any(grp => grp.Count() == 1))
            {
                return true;
            }

            return false;
        }
    }
}
