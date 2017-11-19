using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StatisticsAnalyzerCore.Modeling
{
    public class ModelAnalysis
    {
        public ModelAnalysis() { }

        public ModelAnalysis(ModelAnalysis modelAnalysis)
        {
            RegressionAnalysis = modelAnalysis.RegressionAnalysis;
            AnovaAnalysis = modelAnalysis.AnovaAnalysis;
            Questions = new List<QuestionAnalysis>();
            foreach (QuestionAnalysis analysis in modelAnalysis.Questions)
            {
                Questions.Add(new QuestionAnalysis(analysis));
            }
        }

        [Required]
        public string RegressionAnalysis { get; set; }

        [Required]
        public string AnovaAnalysis { get; set; }

        public List<QuestionAnalysis> Questions { get; set; }
    }
}
