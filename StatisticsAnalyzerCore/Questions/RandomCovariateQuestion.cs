using System.Collections.Generic;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Questions
{
    public class RandomCovariateQuestion : Question
    {
        private readonly string _randomVariable;
        private readonly string _randomCovariate;

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult generalMmodelResult)
        {
            var modelResult = generalMmodelResult.LinearMixedModelResult;

            var comparedModelObj = mixedModel.Clone();
            comparedModelObj.RemoveRandomEffectFormulaVariable(_randomVariable, _randomCovariate);
            var comparedModel = modelResult.ModelComparisons.ComparedModels[comparedModelObj.ModelFormula];

            bool hasSignificantMainInteraction = true; // no main interaction -s so assuming it is "significant" as zero
            if (modelResult.AnovaResult.ContainsKey(new VarGroupIndex(_randomCovariate)))
            {
                hasSignificantMainInteraction = modelResult.AnovaResult[new VarGroupIndex(_randomCovariate)].PValue < StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel;                
            }

            var charts = GetCharts(mixedModel).Where(c => c.Contains(_randomCovariate) && c.Contains(_randomVariable)).First();
            return new Answer
            {
                Question = this,
                AnswerInterpertTemplate = (hasSignificantMainInteraction ?
                                            ("Comparing the given model with a model that omits this covariate we found that " +
                                            ((comparedModel.PValue < StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel) ?
                                                "the variation of the linear connection (slope) between {1} and {2} for diffirent {0} value groups is significant." :
                                                "the linear connection (slope) between {1} and {2} does not vary significantly for different {0} value groups.") +
                                                " We've compared the given model to a model excluding {1} from {0} random group and tested Chi Square value " +
                                                StatisticsTextHelper.CreatePValueReport("X&sup2;",
                                                                                        comparedModel.ChiSq,
                                                                                        comparedModel.PValue) +
                                                " This means, that under the assumption of model validity, after excluding effects from other models' variables " +
                                            ((comparedModel.PValue < StatConfigWrapper.MixedConfig.RandomEffectsConfig.SigLevel) ?
                                                "{1} under {0} random grouping have a significant effect." :
                                                "{1} under {0} random grouping does not have a significant effect.") +
                                            (mixedModel.FixedEffectVariables.Contains(_randomCovariate) ?
                                                string.Empty :
                                                "<b> {1} is not a fixed effect so results could also mean that {1} has a non-zero effect on {2} - see warning section</>")) :
                                            "{1} does not exhibit a significant main effect any variation, analyzing cross interactions in this case is problematic.") +
                                          GetChartElement(charts, dataset.DataTable, mixedModel),
                AnswerParameters = new List<string>
                {
                    _randomVariable,
                    _randomCovariate,
                    mixedModel.PredictedVariable,
                }
            };
        }

        public RandomCovariateQuestion(string randomVariable, string randomCovariate, string predictedVar)
        {
            _randomVariable = randomVariable;
            _randomCovariate = randomCovariate;

            QuestionParameters = new List<string>
            {
                _randomVariable,
                _randomCovariate,
                predictedVar,
            };
            QuestionInterpertTemplate = "Does the linear relationship between {2} and {1} change significantly for different values of {0}?";
        }
    }

    public class RandomCovariateQuestionFactory : QuestionFactory
    {
        public override List<Question> CreateQuestions(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            return mixedModel.RandomEffectVariables
                             .SelectMany(rv => mixedModel.GetRandomLinearFormulas(rv)
                                                         .SelectMany(f => f.AllVariables)
                                                         .Except(new[] {"0", "1"})
                                                         .Select(cv => new RandomCovariateQuestion(rv, cv, mixedModel.PredictedVariable)))
                             .Cast<Question>()
                             .ToList();
        }

        public override bool IsQuestionApplicable(ModelDataset dataset, MixedLinearModel mixedModel)
        {
            if (mixedModel.RandomEffectVariables
                          .Any(rv => mixedModel.GetRandomLinearFormulas(rv)
                                               .SelectMany(f => f.AllVariables)
                                               .Any(cv => cv != "1" && 
                                                          cv != "0")))
            {
                return true;
            }

            return false;
        }

        public RandomCovariateQuestionFactory() : base(QuestionId.DoesXVaryForDifferentY)
        {
        }
    }

}
