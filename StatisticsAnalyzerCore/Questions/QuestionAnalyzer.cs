using System.Collections.Generic;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public class QuestionAnalyzer
    {
        private List<QuestionFactory> questionFactories;

        public QuestionAnalyzer()
        {
            questionFactories = new List<QuestionFactory>();
        }

        public void RegisterQuestionFactory(QuestionFactory questionFactory)
        {
            questionFactories.Add(questionFactory);
        }

        public List<Question> AnalyzeDatasetSupportedQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            var questionList = new List<Question>();

            foreach (var factory in questionFactories)
            {
                List<Question> questions;
                if (factory.TryCreateQuestion(dataset, mixedModel, out questions))
                {
                    questionList.AddRange(questions);
                }
            }

            return questionList;
        }
    }
}
