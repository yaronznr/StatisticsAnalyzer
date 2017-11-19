using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    class MultipleWayAllLevelAnovaQuetion : NWayAnovaQuestion
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var paramList = new List<string>();
            var sb = new StringBuilder();

            sb.Append("We've performed {0}-way {2}. ");
            paramList.Add(VariableList.Count.ToString(CultureInfo.InvariantCulture));
            paramList.Add(QuestionParameters[2]);
            paramList.Add(AnovaNamingHelper.GetAnovaName(dataset, VariableList, false));

            var subGroups = CreateAllSubGroups(VariableList.Count)
                           .Where(grp => grp.Count > 0)
                           .GroupBy(grp => grp.Count)
                           .ToDictionary(grp => grp.Key, grp => grp);

            var stringParamCount = 3;
            var foundSigEffect = false;
            var currentExaminedGroupCount = VariableList.Count;
            while (!foundSigEffect && currentExaminedGroupCount > 0)
            {
                foreach(var subGroup in subGroups[currentExaminedGroupCount])
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
            }

            /*if (foundSigEffect && currentExaminedGroupCount > 0)
            {
                sb.Append(" Effects with less interaction were not examined.");
            }*/

            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = sb.ToString(),
                AnswerParameters = paramList,
            };
        }
    }
}
