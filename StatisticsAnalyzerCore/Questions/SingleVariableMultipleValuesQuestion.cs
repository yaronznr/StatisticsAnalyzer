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
    public class SingleVariableMultipleValuesQuestion : Question
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var dataTable = dataset.DataTable;
            var columnName = mixedModel.FixedEffectVariables.Single();
            var charts = GetCharts(mixedModel).First(c => c.Contains(columnName));

            var continousEffect = modelResult != null ?
                modelResult.FixedEffectResults[new VarGroupIndex(columnName)] :
                generalMmodelResult.BinomialMixedModelResult.FixedEffectResults[new VarGroupIndex(columnName)];
            if (dataTable.Columns[columnName].DataType != typeof(string) || modelResult == null) 
            {
                var effect = continousEffect.EffectResults.First().Value;

                // For binomial models we always use z-scores as we have no ANOVA
                if (modelResult == null)
                {
                    int index = dataset.DataTable.Rows[0].Table.Columns.IndexOf(mixedModel.PredictedVariable);
                    var zeroLevelValue = dataset.DataTable.Rows[0][index] as string;

                    return new Answer
                    {
                        Question = this,
                        AnswerInterpertTemplate = ((Math.Abs(effect.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                        "{0} has a significant effect on {1}. " +
                                                        StatisticsTextHelper.CreateBinomialSlopeStatement(mixedModel.PredictedVariable, zeroLevelValue, columnName, effect.Estimate, false) +
                                                        "After analyzing z-scores we found significant results "
                                                        :
                                                        "{0} does not have a significant effect on {1}. Measuring the linear coefficient we recieve a value of {2}. " +
                                                        "After analyzing z-scores we did not found significant results ") +
                                                  StatisticsTextHelper.CreatePValueReport("Z", effect.TValue, effect.PValue) +
                                                  GetChartElement(charts, dataset.DataTable, mixedModel),
                        AnswerParameters = new List<string>
                            {
                                QuestionParameters[0],
                                QuestionParameters[1],
                                effect.Estimate.ToString(CultureInfo.InvariantCulture),
                            }
                    };
                }

                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = ((Math.Abs(effect.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "{0} has a significant effect on {1}. " +
                                                    StatisticsTextHelper.CreateSlopeStatement(mixedModel.PredictedVariable, columnName, effect.Estimate, false) +
                                                    "After running a student's T-Test we found significant results "
                                                    :
                                                    "{0} does not have a significant effect on {1}. Measuring the linear coefficient we recieve a value of {2}" +
                                                    "After running a student's T-Test we did not found significant results ") +
                                              StatisticsTextHelper.CreatePValueReport("T", effect.TValue, effect.PValue) +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                            {
                                QuestionParameters[0],
                                QuestionParameters[1],
                                effect.Estimate.ToString(CultureInfo.InvariantCulture),
                            }
                };
            }

            var fixedEffect = modelResult.FixedEffectResults[new VarGroupIndex(columnName)];
            if (fixedEffect != null)
            {
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = ((Math.Abs(modelResult.ModelFitResult.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "{0} has a significant effect on {1}. Measuring the F-Value in the model we found " +
                                                    "Measuring the F-Value in the model we found significant results "
                                                    :
                                                    "{0} does not have a significant effect on {1}. " +
                                                    "Measuring the F-Value in the model we did not found significant results ") +
                                              StatisticsTextHelper.CreatePValueReport("F", modelResult.ModelFitResult.FValue, modelResult.ModelFitResult.PValue) +
                                              GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                            {
                                QuestionParameters[0],
                                QuestionParameters[1],
                                modelResult.ModelFitResult.FValue.ToString(CultureInfo.InvariantCulture),
                                modelResult.ModelFitResult.PValue.ToString(CultureInfo.InvariantCulture),
                            }
                };
            }

            throw new MixedModelException("Unexpcted effect level..");
        }
    }

    public class SingleVariableMultipleValuesQuestionFactory : QuestionFactory
    {
        public SingleVariableMultipleValuesQuestionFactory() : base(QuestionId.HowXInfluenceZ) { }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var questions = new List<Question>();

            questions.Add(new SingleVariableMultipleValuesQuestion
            {
                QuestionId = QuestionId,
                QuestionInterpertTemplate = "How does {0} influence {1}",
                QuestionParameters = new List<string> { 
                            mixedModel.FixedEffectVariables.Single(),
                            mixedModel.PredictedVariable,
                        }
            });

            return questions;
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            if (!mixedModel.RandomEffectVariables.Any() &&
                mixedModel.FixedEffectVariables.Count() == 1)
            {
                var columnName = mixedModel.FixedEffectVariables.Single();
                if (dataset.TableStats.ColumnStats[columnName].ValuesCount.Count() > 2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
