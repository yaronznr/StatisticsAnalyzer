using System.ComponentModel.DataAnnotations;

namespace StatisticsAnalyzerCore.Modeling
{
    public class QuestionAnalysis
    {
        public QuestionAnalysis()
        {
        }

        public QuestionAnalysis(QuestionAnalysis questionAnalysis)
        {
            Question = questionAnalysis.Question;
            Answer = questionAnalysis.Answer;
        }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

    }
}
