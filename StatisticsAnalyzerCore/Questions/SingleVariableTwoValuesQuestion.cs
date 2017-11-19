using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    public class SingleVariableTwoValuesQuestion : Question
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;
            var columnName = mixedModel.FixedEffectVariables.Single();

            var effectResult = modelResult != null ?
                modelResult.FixedEffectResults[new VarGroupIndex(columnName)] :
                generalMmodelResult.BinomialMixedModelResult.FixedEffectResults[new VarGroupIndex(columnName)];
            var response = effectResult.EffectResults.First().Value;
            var responseVariable = effectResult.EffectResults.First().Key.ValueNames[0];
            var charts = GetCharts(mixedModel).First(c => c.Contains(columnName));

            if (!mixedModel.RandomEffectVariables.Any())
            {
                if (modelResult == null)
                {
                    int index = dataset.DataTable.Rows[0].Table.Columns.IndexOf(mixedModel.PredictedVariable);
                    var zeroLevelValue = dataset.DataTable.Rows[0][index] as string;

                    return new Answer
                    {
                        Question = this,
                        AnswerInterpertTemplate = ((Math.Abs(response.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                        "Values for {0} and {1} are significantly different. " +
                                                        StatisticsTextHelper.CreateBinomialLargerThanStatement(mixedModel.PredictedVariable,
                                                                                                               zeroLevelValue,
                                                                                                               columnName,
                                                                                                               responseVariable,
                                                                                                               QuestionParameters[0],
                                                                                                               QuestionParameters[1],
                                                                                                               response.Estimate) +
                                                        "After analyzing z-scores we found significant results "
                                                        :
                                                        "Values for {0} and {1} are not significantly different. Average difference between " +
                                                        "the two is {2}. After analyzing z-scores we did not found significant results ")
                                                   + StatisticsTextHelper.CreatePValueReport("Z", response.TValue, response.PValue) +
                                                     GetChartElement(charts, dataset.DataTable, mixedModel),
                        AnswerParameters = new List<string>
                        {
                            QuestionParameters[0],
                            QuestionParameters[1],
                            response.Estimate.ToString(CultureInfo.InvariantCulture),
                            response.TValue.ToString(CultureInfo.InvariantCulture),
                            response.PValue.ToString(CultureInfo.InvariantCulture),
                        }
                    };                                    
                }

                if (modelResult.ModelValidationTest.LeveneTests[new VarGroupIndex(columnName)].PValue <
                    StatConfigWrapper.MixedConfig.AssumptionTestsConfig.LeveneTestConfig.SigLevel &&
                    modelResult.UnequalVarianceTTest != null)
                {
                    return new Answer
                    {
                        Question = this,
                        AnswerInterpertTemplate = "After applying simple T-test we concluded variances are significantly different by applying Levene test " +
                                                  StatisticsTextHelper.CreatePValueReport(modelResult.ModelValidationTest.LeveneTests[new VarGroupIndex(columnName)].PValue) +
                                                  ". We continued our analysis with t-test for unequal variance sizes and found that " +
                                                  ((Math.Abs(modelResult.UnequalVarianceTTest.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                        "values for {0} and {1} are significantly different. " +
                                                        StatisticsTextHelper.CreateLargerThanStatement(responseVariable, 
                                                                                                       QuestionParameters[0],
                                                                                                       QuestionParameters[1],
                                                                                                       response.Estimate) +
                                                        "After running a student's T-Test we found significant results ":
                                                        "values for {0} and {1} are not significantly different. Average difference between " +
                                                        "the two is {2}. " +
                                                        "After running a student's T-Test we did not found significant results ") +
                                                  StatisticsTextHelper.CreatePValueReport("T", response.TValue, response.PValue) +
                                                  GetChartElement(charts, dataset.DataTable, mixedModel),
                        AnswerParameters = new List<string>
                        {
                            QuestionParameters[0],
                            QuestionParameters[1],
                            Math.Abs(response.Estimate).ToString(CultureInfo.InvariantCulture),
                            response.TValue.ToString(CultureInfo.InvariantCulture),
                            response.PValue.ToString(CultureInfo.InvariantCulture),
                        }
                    };                                                        
                }

                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = ((Math.Abs(response.PValue) <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                    "Values for {0} and {1} are significantly different. " +
                                                    StatisticsTextHelper.CreateLargerThanStatement(responseVariable, 
                                                                                                   QuestionParameters[0],
                                                                                                   QuestionParameters[1],
                                                                                                   response.Estimate) +
                                                    "After running a student's T-Test we found significant results "
                                                    :
                                                    "Values for {0} and {1} are not significantly different. Average difference between " +
                                                    "the two is {2}. After running a student's T-Test we did not found significant results ")
                                               + StatisticsTextHelper.CreatePValueReport("T", response.TValue, response.PValue) +
                                                 GetChartElement(charts, dataset.DataTable, mixedModel),
                    AnswerParameters = new List<string>
                    {
                        QuestionParameters[0],
                        QuestionParameters[1],
                        response.Estimate.ToString(CultureInfo.InvariantCulture),
                        response.TValue.ToString(CultureInfo.InvariantCulture),
                        response.PValue.ToString(CultureInfo.InvariantCulture),
                    }
                };                                    
            }

            var krAnovaResult = modelResult.AnovaResult[new VarGroupIndex(columnName)];
                
            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = ((krAnovaResult.PValue <= StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                                                "Values for {0} and {1} are significantly different. " +
                                                StatisticsTextHelper.CreateLargerThanStatement(responseVariable,
                                                                                               QuestionParameters[0],
                                                                                               QuestionParameters[1],
                                                                                               response.Estimate) +
                                                "After running a {3}F-Test with Kenward-Rogers DF we found significant results "
                                                :
                                                "Values for {0} and {1} are not significantly different. Average difference between " +
                                                "the two is {2}. After running a {3}F-Test with Kenward-Rogers DF we did not found significant results ")
                                            + StatisticsTextHelper.CreatePValueReport("F", krAnovaResult.FValue, krAnovaResult.PValue) +
                                             GetChartElement(charts, dataset.DataTable, mixedModel),
                AnswerParameters = new List<string>
                    {
                        QuestionParameters[0],
                        QuestionParameters[1],
                        response.Estimate.ToString(CultureInfo.InvariantCulture),
                        AnovaNamingHelper.GetAnovaName(dataset, new List<string> { columnName }, true),
                    }
            };                                
        }
    }

    public class SingleVariableTwoValuesQuestionFactory : QuestionFactory
    {
        public SingleVariableTwoValuesQuestionFactory() : base(QuestionId.DoXandYHaveDifferentZ) { }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var questions = new List<Question>();

            var columnName = mixedModel.FixedEffectVariables.Single();
            questions.Add(new SingleVariableTwoValuesQuestion
            {
                QuestionId = QuestionId,
                QuestionInterpertTemplate = "Do {0} and {1} have different values for {2}",
                QuestionParameters = new List<string> { 
                            dataset.TableStats.ColumnStats[columnName].ValuesCount.Keys.ElementAt(0).ToString(), 
                            dataset.TableStats.ColumnStats[columnName].ValuesCount.Keys.ElementAt(1).ToString(),
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
                if (dataset.TableStats.ColumnStats[columnName].ValuesCount.Count() == 2 &&
                    dataset.DataTable.Columns[columnName].DataType == typeof(string))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
