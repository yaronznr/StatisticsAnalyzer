using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    class SimpleRandomEffectBiasQuestion : Question
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalModelResult)
        {
            var comparedModelObj = mixedModel.Clone();
            comparedModelObj.RemoveRandomEffectPart(QuestionParameters[0]);

            ModelComparison comparedModel;
            if (generalModelResult.LinearMixedModelResult != null)
            {
                comparedModel = generalModelResult.LinearMixedModelResult.ModelComparisons.ComparedModels.ContainsKey(QuestionParameters[0]) ?
                    generalModelResult.LinearMixedModelResult.ModelComparisons.ComparedModels[QuestionParameters[0]] :        // Single random effect case 
                    generalModelResult.LinearMixedModelResult.ModelComparisons.ComparedModels[comparedModelObj.ModelFormula]; // Multiple random effect case
            }
            else
            {
                comparedModel = generalModelResult.BinomialMixedModelResult.ModelComparisons.ComparedModels.ContainsKey(QuestionParameters[0]) ?
                    generalModelResult.BinomialMixedModelResult.ModelComparisons.ComparedModels[QuestionParameters[0]] :        // Single random effect case 
                    generalModelResult.BinomialMixedModelResult.ModelComparisons.ComparedModels[comparedModelObj.ModelFormula]; // Multiple random effect case
            }

            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = "We've compared the given model to a model excluding {0} random effect and found " +
                                            ((comparedModel.PValue < StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel) ?
                                            "{1} vary significantly for different values of {0} " :
                                            "{1} does not vary significantly for different values of {0} ") +
                                            StatisticsTextHelper.CreatePValueReport("X&sup2;",
                                                                                    comparedModel.ChiSq,
                                                                                    comparedModel.PValue) + ".",
                AnswerParameters = new List<string>
                {
                    QuestionParameters[0],
                    mixedModel.PredictedVariable,
                    comparedModel.ChiSq.ToString(CultureInfo.InvariantCulture),
                    comparedModel.PValue.ToString(CultureInfo.InvariantCulture),
                }
            };
        }
    }

    public class SimpleRandomEffectBiasQuestionFactory : QuestionFactory
    {
        public SimpleRandomEffectBiasQuestionFactory() : base(QuestionId.HowXInfluenceZ)
        {
        }

        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return mixedModel.RandomEffectVariables.Select(randomEffectVariable => new SimpleRandomEffectBiasQuestion
            {
                QuestionId = QuestionId.HowXInfluenceZ,
                QuestionInterpertTemplate = "Does the expected value of {1} vary significantly for different values of {0}", 
                QuestionParameters = new List<string>
                {
                    randomEffectVariable,
                    mixedModel.PredictedVariable,
                }
            }).Cast<Question>().ToList();
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return mixedModel.RandomEffectVariables.Any();
        }
    }
}
