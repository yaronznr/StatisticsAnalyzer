using System.Collections.Generic;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    public class ThreeWayTwoLevelAnovaQuetion : MultipleWayAllLevelAnovaQuetion
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult modelResult)
        {
            var interaction3Result = modelResult.AnovaResult[new VarGroupIndex(VariableList)];

            if (interaction3Result.PValue < StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel)
            {
                var paramList = new List<string>();
                var sb = new StringBuilder();

                sb.Append("We've performed 3-way {0} and found a significant 3-way interaction effect {1}.");
                paramList.Add(AnovaNamingHelper.GetAnovaName(dataset, VariableList, false));
                paramList.Add(StatisticsTextHelper.CreatePValueReport("F", interaction3Result.FValue, interaction3Result.PValue));

                string value1Report = "Blah blah. ";
                string value2Report = "FooBar. ";

                sb.Append(value1Report);
                sb.Append(value2Report);
                /*
                var subGroups = CreateAllSubGroups(VariableList.Count)
                               .Where(grp => grp.Count > 0)
                               .GroupBy(grp => grp.Count)
                               .ToDictionary(grp => grp.Key, grp => grp);

                var stringParamCount = 2;
                var foundSigEffect = false;
                var currentExaminedGroupCount = VariableList.Count;
                while (!foundSigEffect && currentExaminedGroupCount > 0)
                {
                    foreach (var subGroup in subGroups[currentExaminedGroupCount])
                    {
                        var currentVarList = subGroup.Select(index => VariableList[index]).ToList();
                        var anovaResult = modelResult.AnovaResult[new VarGroupIndex(currentVarList)];

                        if (currentExaminedGroupCount > 1)
                        {
                            sb.Append(string.Format("We examined interaction effect between variables ({0}) on {1} ",
                                                    FormatterIndex(stringParamCount++),
                                                    FormatterIndex(1)));
                        }
                        else
                        {
                            sb.Append(string.Format("We examined main effect of variable {0} on {1} ",
                                                    FormatterIndex(stringParamCount++),
                                                    FormatterIndex(1)));
                        }
                        paramList.Add(string.Join(",", currentVarList));

                        if (anovaResult.PValue < StatConfigWrapper.MixedConfig.FixedEffectConfig.SigLevel)
                        {
                            foundSigEffect = true;
                            sb.Append(" and found a significant effect ");
                        }
                        else
                        {
                            sb.Append(" and found no siginificant effect ");
                        }

                        sb.Append(StatisticsTextHelper.CreatePValueReport("F",
                                                                          anovaResult.FValue,
                                                                          anovaResult.PValue));
                    }

                    currentExaminedGroupCount--;
                }*/

                return new Answer
                {
                    Question = this,
                    AnswerInterpertTemplate = sb.ToString(),
                    AnswerParameters = paramList,
                };
            }
            
            return base.AnalyzeAnswer(dataset, mixedModel, modelResult);
        }
    }
}
