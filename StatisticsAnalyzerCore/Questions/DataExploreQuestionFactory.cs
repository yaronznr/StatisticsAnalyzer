using System.Collections.Generic;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public class DataExploreQuestionFactory : QuestionFactory
    {
        public DataExploreQuestionFactory()
            : base(QuestionId.DataExplore)
        {
        }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return new List<Question> { 
                new DataExploreQuestion
                {
                    QuestionId = QuestionId,
                    QuestionInterpertTemplate = "The Data:",
                    QuestionParameters = new List<string>(),
                }
            };
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return true;
        }
    }
}
