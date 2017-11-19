using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public abstract class NWayAnovaQuestion : Question
    {
        protected IEnumerable<List<int>> CreateAllSubGroups(int size)
        {
            if (size == 0)
            {
                yield return new List<int>();
                yield break;
            }

            foreach (var subGroups in CreateAllSubGroups(size - 1))
            {
                yield return new List<int>(subGroups);

                subGroups.Add(size - 1);
                yield return subGroups;
            }
        }

        protected string FormatterIndex(int index)
        {
            return "{" + index + "}";
        }

        public  List<string> VariableList { get; set; }
    }

    public class NWayAnovaQuestionFactory : QuestionFactory
    {
        public NWayAnovaQuestionFactory() : base(QuestionId.HowVariablesInfluenceZ) { }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var questions = new List<Question>();
            var dataTable = dataset.DataTable;

            // Annalyze 2-way anova in depth
            foreach (var fixedVarGroup in mixedModel.GetFixedLinearFormula()
                                                    .VariableGroups
                                                    .Where(grp => grp.Count() == 2)
                                                    .Select(grp => grp.ToList()))
            {
                if (fixedVarGroup.All(c => dataTable.Columns[c].DataType == typeof(string)))
                {
                    questions.Add(new TwoWay22AnovaQuestion
                    {
                        QuestionId = QuestionId,
                        QuestionInterpertTemplate = "How are {0} and {1} influencing {2}",
                        QuestionParameters = new List<string> { 
                            fixedVarGroup.ElementAt(0),
                            fixedVarGroup.ElementAt(1),
                            mixedModel.PredictedVariable,
                        },
                        VariableList = fixedVarGroup.ToList(),
                    });
                }
                else
                {
                    var lastVar = fixedVarGroup.Last();
                    questions.Add(new TwoWayContinousVariableAnovaQuestion
                    {
                        QuestionId = QuestionId,
                        QuestionInterpertTemplate = "How are {0} and {1} influencing {2}",
                        QuestionParameters = new List<string> { 
                            string.Join(", ", fixedVarGroup.Where(varGrp => varGrp != lastVar)),
                            lastVar,
                            mixedModel.PredictedVariable,
                        },
                        VariableList = fixedVarGroup.ToList(),
                    });                    
                }
            }

            // Do simple reports for n-way anova (n>2)
            foreach (var fixedVarGroup in mixedModel.GetFixedLinearFormula()
                                                    .VariableGroups
                                                    .Where(grp => grp.Count() > 2)
                                                    .Select(grp => grp.ToList()))
            {
                var lastVar = fixedVarGroup.Last();

                if (dataTable.Columns[lastVar].DataType == typeof(string))
                {
                    questions.Add(new MultipleWayAllLevelAnovaQuetion
                    {
                        QuestionId = QuestionId,
                        QuestionInterpertTemplate = "How are {0} and {1} influencing {2}",
                        QuestionParameters = new List<string> { 
                            string.Join(", ", fixedVarGroup.Where(varGrp => varGrp != lastVar)),
                            lastVar,
                            mixedModel.PredictedVariable,
                        },
                        VariableList = fixedVarGroup.ToList(),
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
                mixedModel.GetFixedLinearFormula().VariableGroups.Any(grp => grp.Count() > 1))
            {
                return dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType != typeof(string);
            }

            return false;
        }
    }
}
