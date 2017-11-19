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
    class TwoWay22AnovaQuestion : NWayAnovaQuestion
    {
        private string AddInteractionAnalysis(string var1, 
                                              string var2,
                                              ModelDataset dataset,
                                              MixedLinearModel mixedModel,
                                              MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var allVars = new List<string> {var1, var2};
            var twoValueVars = allVars.
                Where(v => dataset.TableStats.ColumnStats[v].ValuesCount.Count == 2).
                ToList();

            bool hasTwoTwoValueVars = twoValueVars.Count == 2;
            if (twoValueVars.Any())
            {
                var twoValueVar = twoValueVars.First();
                var otherVar = allVars.Except(new List<string> {twoValueVar}).Single();
                var twoValueVarValue1 = dataset.TableStats.ColumnStats[twoValueVar].ValuesCount.Keys.ElementAt(0).ToString();
                var twoValueVarValue2 = dataset.TableStats.ColumnStats[twoValueVar].ValuesCount.Keys.ElementAt(1).ToString();

                var formula = mixedModel.GetFixedLinearFormula()
                                        .VariableGroups
                                        .Where(f =>
                                        {
                                            var arr = f as string[] ?? f.ToArray();
                                            return arr.Count() == 2 && arr.Contains(twoValueVar) && arr.Contains(otherVar);
                                        })
                                        .First();
                var twoValueVarIndex = formula.First() == otherVar ? 1 : 0;

                var baseDiff = modelResult.FixedEffectResults[new VarGroupIndex(twoValueVar)].EffectResults.First().Value.Estimate;
                var baseReponse = modelResult.FixedEffectResults[new VarGroupIndex(twoValueVar)].EffectResults.First().Key.ValueNames[0];
                var interactionEffects = modelResult.FixedEffectResults[new VarGroupIndex(allVars)].EffectResults.OrderBy(e => e.Value.PValue).First();
                var interactionValue = interactionEffects.Key.ValueNames[1-twoValueVarIndex];
                var interactionValueEffect = interactionEffects.Value.Estimate;
                var interactionOtherValue = dataset.TableStats
                                                   .ColumnStats[otherVar]
                                                   .ValuesCount
                                                   .Keys
                                                   .Except(modelResult.FixedEffectResults[new VarGroupIndex(allVars)]
                                                                      .EffectResults
                                                                      .Keys
                                                                      .Select(v => v.ValueNames[1 - twoValueVarIndex]))
                                                   .First();
                return string.Format("This means that for different values of {0}, effect of {1} is different. {7}" +
                                     "hen {0} is {2}, {4} And when {0} is {3}, {5}The difference in effect between {2} and {3} is {6} {8}. ",
                                     otherVar,
                                     twoValueVar,
                                     interactionOtherValue,
                                     interactionValue,
                                     StatisticsTextHelper.CreateLargerThanStatement(baseReponse, twoValueVarValue2, twoValueVarValue1, baseDiff),
                                     StatisticsTextHelper.CreateLargerThanStatement(baseReponse, twoValueVarValue2, twoValueVarValue1, baseDiff + interactionValueEffect),
                                     Math.Abs(modelResult.FixedEffectResults[new VarGroupIndex(allVars)].EffectResults.First().Value.Estimate),
                                     hasTwoTwoValueVars ? "W" : "For Example, w",
                                     mixedModel.RandomEffectVariables.Any() ? (hasTwoTwoValueVars ?
                                                            StatisticsTextHelper.CreatePValueReport("F",
                                                                                                    modelResult.AnovaResult[new VarGroupIndex(allVars)].FValue,
                                                                                                    modelResult.AnovaResult[new VarGroupIndex(allVars)].PValue) :
                                                            "") :
                                                          StatisticsTextHelper.CreatePValueReport("T",
                                                                                                    interactionEffects.Value.TValue,
                                                                                                    interactionEffects.Value.PValue));
            }

            return "";
        }

        private string AddMainEffectAnalysis(string variable, ModelDataset dataset, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var columnStat = dataset.TableStats.ColumnStats[variable];
            if (columnStat.ValuesCount.Count == 2)
            {
                var response = modelResult.FixedEffectResults[new VarGroupIndex(variable)].EffectResults.First();
                return StatisticsTextHelper.CreateLargerThanStatement(response.Key.ValueNames[0],
                                                                      columnStat.ValuesCount.Keys.ElementAt(0).ToString(),
                                                                      columnStat.ValuesCount.Keys.ElementAt(1).ToString(),
                                                                      response.Value.Estimate);
            }

            return "";
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var interactionAnovaResult = modelResult.AnovaResult[new VarGroupIndex(VariableList)];
            var var1AvonaResult = modelResult.AnovaResult[new VarGroupIndex(VariableList[0])];
            var var2AvonaResult = modelResult.AnovaResult[new VarGroupIndex(VariableList[1])];
            if (interactionAnovaResult.PValue < StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel)
            {
                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "{0} and {1} have a significant effect on {2}. We have preformed 2-way {3} and found a significant interaction effect " +
                                              StatisticsTextHelper.CreatePValueReport("F", interactionAnovaResult.FValue, interactionAnovaResult.PValue) + ". " +
                                              AddInteractionAnalysis(VariableList[0], VariableList[1], dataset, mixedModel, generalMmodelResult) + 
                                              "Main effect influence was not analysed due to the presence of an interaction effect.",
                    AnswerParameters = new List<string>
                            {
                                VariableList[0],
                                VariableList[1],
                                QuestionParameters[2],
                                AnovaNamingHelper.GetAnovaName(dataset, VariableList, false),
                            }
                };
            }
            
            if (var1AvonaResult.PValue < 0.05 || var2AvonaResult.PValue < 0.05)
            {
                var mainEffect1String = ((var1AvonaResult.PValue < StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                    "{0} has a significant effect on {2} " +
                    StatisticsTextHelper.CreatePValueReport("F", var1AvonaResult.FValue, var1AvonaResult.PValue) + ". " +
                    AddMainEffectAnalysis(VariableList[0], dataset, generalMmodelResult) :
                    ("{0} did not display a significant effect on {2} " +
                    StatisticsTextHelper.CreatePValueReport("F", var1AvonaResult.FValue, var1AvonaResult.PValue) + ". "));

                var mainEffect2String = ((var2AvonaResult.PValue < StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel) ?
                    "{1} has a significant effect on {2} " +
                    StatisticsTextHelper.CreatePValueReport("F", var2AvonaResult.FValue, var2AvonaResult.PValue) + ". " +
                    AddMainEffectAnalysis(VariableList[1], dataset, generalMmodelResult) :
                    ("{1} did not display a significant effect on {2} " +
                    StatisticsTextHelper.CreatePValueReport("F", var2AvonaResult.FValue, var2AvonaResult.PValue) + ". "));

                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = "We have performed 2-way {3} and found that " +
                                              mainEffect1String + mainEffect2String +
                                              "No significant interaction effect was found " +
                                              StatisticsTextHelper.CreatePValueReport(interactionAnovaResult.PValue),
                    AnswerParameters = new List<string>
                    {
                        VariableList[0],
                        VariableList[1],
                        QuestionParameters[2],
                        AnovaNamingHelper.GetAnovaName(dataset, VariableList, false),
                    }
                };
            }
            
            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = "{0} and {1} don't have a significant effect on {2}. We have preformed 2-way {5} and the found interaction " +
                                            "effect was not significant (F={3},P.value={4}). Main effect we also tested and no significant effect were found.",
                AnswerParameters = new List<string>
                        {
                            VariableList[0],
                            VariableList[1],
                            QuestionParameters[2],
                            interactionAnovaResult.FValue.ToString(CultureInfo.InvariantCulture),
                            interactionAnovaResult.PValue.ToString(CultureInfo.InvariantCulture),
                            AnovaNamingHelper.GetAnovaName(dataset, VariableList, false),
                        }
            };
        }
    }
}
