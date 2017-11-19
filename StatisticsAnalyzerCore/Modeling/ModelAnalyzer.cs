using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Questions;
using StatisticsAnalyzerCore.StatConfig;

namespace StatisticsAnalyzerCore.Modeling
{
    public class ModelInsight
    {
        public enum ModelInsightCode
        {
            None,
            BalancedData,
            CrossedBalanced,
            NestedVar,
            RandomVarNotRepeated,
            RandomVarFewLevels,
            CovariateMissingMainEffect,
            CovariateNotCovering,
            RandomVarWithoutContant,
            RandomVarHasCorrelation,
            NoApplicableRows,
            FewApplicableRows,
        }

        public enum ModelInsightSeverity
        {
            Notice,
            Warning,
            Error,
        }

        public ModelInsightCode InsightCode { get; private set; }
        public string InsightText { get; private set; }
        public List<object> InsightParameters { get; private set; }
        public ModelInsightSeverity InsightSeverity { get; set; }

        public ModelInsight(ModelInsightCode code, string text, ModelInsightSeverity insightSeverity, params object[] insightParams)
        {
            InsightCode = code;
            InsightText = text;
            InsightSeverity = insightSeverity;
            InsightParameters = insightParams.ToList();
        }
    }

    public static class ModelAnalyzer
    {
        private static string CreateModelSummary(MixedLinearModel mixedModel, ModelDataset dataset)
        {
            var sb = new StringBuilder();
            var tableStats = dataset.TableStats;

            var fixedVars = mixedModel.GetFixedLinearFormula().AllVariables;
            var randomVars = mixedModel.RandomEffectVariables;

            var vars = fixedVars as string[] ?? fixedVars.ToArray();
            var randomVarArr = randomVars as string[] ?? randomVars.ToArray();
            if ((vars.Any(e => !tableStats.ColumnStats.Keys.Contains(e)) ||
                randomVarArr.Any(e => !tableStats.ColumnStats.Keys.Contains(e))) &&
                !mixedModel.HasIterator)
            {
                var missingVariable = vars.First(e => !tableStats.ColumnStats.Keys.Contains(e));
                sb.Append("Missing variable \"");
                sb.Append(missingVariable);
                sb.Append("\"");
            }
            else
            {
                var isBinomial = dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType == typeof(string);
                var regularSummaryOpen = isBinomial ?
                    string.Format("In this model, it is assumed that the odds of \"{0}\" taking a specific value is affected by ", mixedModel.PredictedVariable) :
                    string.Format("In this model, it is assumed that the value of \"{0}\" is affected by ", mixedModel.PredictedVariable);

                if (vars.Count() == 1 &&
                    tableStats.ColumnStats.ContainsKey(vars.First()) &&
                    tableStats.ColumnStats[vars.First()].ValuesCount.Count == 2)
                {
                    sb.Append("Do ");
                    sb.Append(string.Join(" and ", tableStats.ColumnStats[vars.First()].ValuesCount.Keys));
                    if (isBinomial)
                    {
                        sb.AppendFormat(" have different odds for {0} taking a specific value", mixedModel.PredictedVariable);
                    }
                    else
                    {
                        sb.Append(" have different values for ");
                        sb.Append(mixedModel.PredictedVariable);
                    }
                    sb.Append(".");
                }
                else if (vars.Count() == 1)
                {
                    sb.Append(regularSummaryOpen);
                    sb.Append("one element: ");
                    sb.Append(string.Join(",", mixedModel.GetFixedLinearFormula().AllVariables));
                    sb.Append(".");
                }
                else if (mixedModel.GetFixedLinearFormula().VariableGroups.Count() == 1)
                {
                    sb.Append(regularSummaryOpen);
                    sb.Append("variables: ");
                    sb.Append(string.Join(", ", mixedModel.GetFixedLinearFormula().AllVariables));
                    sb.Append(" in such a way that every combination of values yields a separated response for the value of \"");
                    sb.Append(mixedModel.PredictedVariable);
                    sb.Append("\".");
                }
                else if (mixedModel.GetFixedLinearFormula().VariableGroups.Any() &&
                         mixedModel.GetFixedLinearFormula().VariableGroups.All(vg => vg.Count() == 1))
                {
                    sb.Append(regularSummaryOpen);
                    sb.Append("variables: ");
                    sb.Append(string.Join(", ", mixedModel.GetFixedLinearFormula().AllVariables));
                    sb.Append(". We measure both main effects (effect derived for the variation of a single fixed effect).");
                }
                else
                {
                    sb.Append("In this model, we consider variables (");
                    sb.Append(string.Join(", ", MixedModelHelper.GetAllAffectingVariables(mixedModel)));
                    if (isBinomial)
                    {
                        sb.Append(") as effecting odds of ");
                        sb.Append(mixedModel.PredictedVariable);
                        sb.Append(" taking a specific value");
                    }
                    else
                    {
                        sb.Append(") as effecting value of ");
                        sb.Append(mixedModel.PredictedVariable);
                    }
                    sb.Append(". We measure the main effects (effect derived for the variation of a single fixed effect). We also measure ");
                    sb.Append("interactions (effects that are a result of two/more fixed effects being assigned a value) between ");
                    sb.Append(string.Join(", ", mixedModel.GetFixedLinearFormula()
                                                          .VariableGroups
                                                          .Where(grp => grp.Count() > 1)
                                                          .Select(grp => string.Join(" and ", grp))));
                    sb.Append(". Other interactions are assumed not to be present.");
                }

                if (randomVarArr.Any())
                {
                    sb.Append(" In addition, ");
                    if (randomVarArr.All(r => mixedModel.GetRandomLinearFormula(r).AllVariables.Count() == 1 &&
                                            mixedModel.GetRandomLinearFormula(r).AllVariables.Single() == "1"))
                    {
                        sb.Append("we've modeled the ");
                        sb.Append(string.Join(", ", mixedModel.RandomEffectVariables.Select(str => string.Format("by-{0} variation", str))));
                        sb.Append(" as ");
                        sb.Append(mixedModel.RandomEffectVariables.Count() > 1 ? "random effects" : "a random effect");
                        sb.Append(". This is mainly done in order to exclude this variation from the statistical significance of fixed variables");
                        sb.Append(" effects when performing the various statistical tests.");
                    }
                    else
                    {
                        var isFirst = true;
                        foreach (var randomVar in randomVarArr)
                        {
                            var randomFormula = mixedModel.GetRandomLinearFormula(randomVar);

                            sb.Append(string.Format(" {0} modeled the ", isFirst ? "we've" : "And "));
                            sb.Append(string.Format("by-{0} variation", randomVar));
                            sb.Append(" as a random effect");
                            sb.Append(randomFormula.AllVariables.Count() == 1
                                ? string.Empty
                                : string.Format(" including a random bias and random slope for {0}",
                                    randomFormula.AllVariables.Count() == 2
                                        ? randomFormula.AllVariables.First(v => v != "1" && v != "0")
                                        : "several variables "));
                            sb.Append(string.Format(" assigned on each value of {0}. This is mainly done in order to exclude this variation from the statistical significance of fixed variables", randomVar));
                            sb.Append(" effects when performing the various statistical tests.");

                            isFirst = false;
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<ModelInsight> CreateModelInsights(MixedLinearModel mixedModel, ModelDataset dataset)
        {
            var tableAnalysis = dataset.TableStats.TableAnalysis;

            var nestingDetected = false;
            var balanceDetected = false;

            var variables = mixedModel.AllVariables.Concat(new List<string> {mixedModel.PredictedVariable}).ToList();
            var fullTableCount = dataset.DataTable.Rows.Count;
            var nonNullCount = dataset.DataTable.Rows.Cast<DataRow>().Count(r => !variables.Any(v => r[v].IsNull()));

            if (nonNullCount == 0)
            {
                yield return new ModelInsight(
                    ModelInsight.ModelInsightCode.NoApplicableRows,
                    "Each row has missing data - no rows will be counted!",
                    ModelInsight.ModelInsightSeverity.Error);
                yield break;
            }
            if (nonNullCount*1.0/fullTableCount < 0.8)
            {
                yield return new ModelInsight(
                    ModelInsight.ModelInsightCode.FewApplicableRows,
                    string.Format(
                        "Many row were ommited since they are missing some values. Working on {0}% of values",
                        (nonNullCount * 100.0 / fullTableCount).ToString("n2")),
                    ModelInsight.ModelInsightSeverity.Warning,
                    nonNullCount * 100.0 / fullTableCount);
            }

            var fixedVars = new HashSet<string>(mixedModel.FixedEffectVariables);
            if (fixedVars.Count() == 1)
            {
                var fixedVar = fixedVars.First();
                if (tableAnalysis.ColumnBalanaces.ContainsKey(fixedVar) &&
                    tableAnalysis.ColumnBalanaces[fixedVar] == ColumnBalanace.Balanced)
                {
                    balanceDetected = true;
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.BalancedData,
                        string.Format("Data is balanced on {0}.<br>", fixedVar),
                        ModelInsight.ModelInsightSeverity.Notice,
                        fixedVar);
                }
            }
            if (fixedVars.Count() == 2)
            {
                var fixedVar1 = fixedVars.First();
                var fixedVar2 = fixedVars.Skip(1).First();
                if ((tableAnalysis.ColumnBalanaces.ContainsKey(fixedVar1) &&
                     tableAnalysis.ColumnBalanaces[fixedVar1] == ColumnBalanace.Balanced &&
                     tableAnalysis.CrossBalanaceGroup[fixedVar1].Contains(fixedVar2)) ||
                    (tableAnalysis.ColumnBalanaces.ContainsKey(fixedVar2) &&
                     tableAnalysis.ColumnBalanaces[fixedVar2] == ColumnBalanace.Balanced &&
                     tableAnalysis.CrossBalanaceGroup[fixedVar2].Contains(fixedVar1)))
                {
                    balanceDetected = true;
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.CrossedBalanced,
                        string.Format("Data is crossed balanced on both {0} and {1}. ", fixedVar1, fixedVar2) +
                        ((mixedModel.GetFixedLinearFormula().VariableGroups.Count > 1) ?
                            "Since data is fully crossed balanced. This might suggest original intent was to also measure " +
                            string.Format("interactions. Consider trying: \"{0}\" for the fixed part.<br>", string.Join("*", fixedVars)) :
                            "<br>"),
                        ModelInsight.ModelInsightSeverity.Notice,
                        fixedVar1,
                        fixedVar2);
                }
            }

            var varSet = new HashSet<string>(mixedModel.AllVariables);
            foreach (var modelVar in varSet)
            {
                if (tableAnalysis.ColumnGraph.ContainsKey(modelVar))
                {
                    var modelVarRelations = tableAnalysis.ColumnGraph[modelVar];
                    var nestedVars = modelVarRelations.Where(kvp => kvp.Value.RelationAttributes.Contains(LinkAttributes.Nested))
                                                      .Where(kvp => varSet.Contains(kvp.Key))
                                                      .ToList();

                    if (nestedVars.Any())
                    {
                        nestingDetected = true;
                        if (fixedVars.Contains(modelVar))
                        {
                            yield return new ModelInsight(
                                ModelInsight.ModelInsightCode.NestedVar,
                                string.Format("{0} is nested under ", modelVar) +
                                         (nestedVars.Count == 1
                                             ? string.Format("{0}<br>", nestedVars.First().Key)
                                             : string.Format("several variables (e.g. {0})<br>", nestedVars.First())),
                                ModelInsight.ModelInsightSeverity.Notice,
                                modelVar,
                                nestedVars.Select(kvp => kvp.Key).ToList());
                        }
                    }

                    string mVar = modelVar;
                    var outOfModelNestedVars = 
                        dataset.TableStats
                               .ColumnStats
                               .Keys
                               .Where(v => dataset.TableStats.TableAnalysis.ColumnGraph.ContainsKey(v))
                               .Where(v => dataset.TableStats.TableAnalysis.ColumnGraph[v].ContainsKey(mVar))
                               .Where(v => dataset.TableStats.TableAnalysis.ColumnGraph[v][mVar].RelationAttributes.Contains(LinkAttributes.Nested))
                               .ToList();
                    if (outOfModelNestedVars.Any())
                    {
                        nestingDetected = true;
                        foreach (var outOfModelNestedVar in outOfModelNestedVars)
                        {
                            if (!varSet.Contains(outOfModelNestedVar) &&
                                dataset.TableStats.ColumnStats[outOfModelNestedVar].ValuesCount.Count >= 5 &&
                                (dataset.TableStats.TableAnalysis.ColumnRepeated[outOfModelNestedVar] == ColumnRepeated.StrictRepeated ||
                                 dataset.TableStats.ColumnStats[outOfModelNestedVar].ValuesCount.Count(x => x.Value == 1) <
                                 0.5*dataset.TableStats.ColumnStats[outOfModelNestedVar].ValuesCount.Count))
                            {
                                if (outOfModelNestedVars.Count == 1)
                                {
                                    var randomEffectModel = mixedModel.Clone();
                                    randomEffectModel.AndRandomEffect(outOfModelNestedVar);
                                    yield return new ModelInsight(
                                        ModelInsight.ModelInsightCode.NestedVar,
                                        string.Format(
                                            "{0} is nested under {1}. This is a strong indication that {1} should " +
                                            "be included as a random effect. Consider using '{2}'. Without this " +
                                            "the model would not generalize to other values of {1}",
                                            modelVar,
                                            outOfModelNestedVar,
                                            Question.GetChangeModel(randomEffectModel.ModelFormula)),
                                        ModelInsight.ModelInsightSeverity.Warning,
                                        modelVar,
                                        new List<string> { outOfModelNestedVar });
                                }
                            }                                                    
                        }
                    }

                }
            }

            var missingMainEffects = new HashSet<string>();
            var nonCoveringCovariates = new HashSet<Tuple<string, string>>();
            foreach (var randomEffectVariable in mixedModel.RandomEffectVariables)
            {
                if (!mixedModel.GetRandomLinearFormulas(randomEffectVariable).Any(f => f.AllVariables.Contains("1")))
                {
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.RandomVarWithoutContant,
                        string.Format("{0} is specified as a random grouping variable without an intercept. This is " + 
                                      "usually not recomended. When we assume {0} affects the slope we usually also assume it " +
                                      "affects the intercet. Sometime it might even appear for artificial reasons so it is always" +
                                      "recommmended to be included in the model. <br>",
                                    randomEffectVariable),
                        ModelInsight.ModelInsightSeverity.Warning,
                        randomEffectVariable,
                        dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Where(kvp => kvp.Value == 1).ToString());
                }

                var groups = mixedModel.GetRandomLinearFormulas(randomEffectVariable)
                    .Where(f => f.AllVariables.Contains("1") && f.AllVariables.Count() > 1)
                    .ToList();
                if (groups.Any())
                {
                    var groupedCount = groups.Count;
                    var clonedModel = mixedModel.Clone();
                    clonedModel.DecorrelateAllCovariates();

                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.RandomVarHasCorrelation,
                        string.Format("There {0} where the assumption is there is a correlation between random intercept " + 
                                      "and random slope in element \"{1}\". This is usually not needed since we center random covariates. " +
                                      "If this was not the original intention, the following model is proposed: {2}",
                                      groupedCount == 1 ? "is one element" : string.Format("are {0} elements", groupedCount),
                                      string.Format("{0}|{1}", string.Join(" + ", groups[0].AllVariables), randomEffectVariable),
                                      Question.GetChangeModel(clonedModel.ModelFormula)),
                        ModelInsight.ModelInsightSeverity.Warning);
                }

                if (tableAnalysis.ColumnRepeated[randomEffectVariable] != ColumnRepeated.StrictRepeated)
                {
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.RandomVarNotRepeated,
                        string.Format("{0} is not repeated in several levels (e.g. {1}). This could reduce the model's power.<br>",
                                    randomEffectVariable,
                                    dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.First(kvp => kvp.Value == 1).Key),
                        ModelInsight.ModelInsightSeverity.Warning,
                        randomEffectVariable,
                        dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Where(kvp => kvp.Value == 1).ToString());
                }
                if (dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Count < StatConfigWrapper.MixedConfig.RandomEffectsConfig.RandomLevelCountsWarn)
                {
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.RandomVarFewLevels,
                        string.Format("{0} has {1} levels. This makes it hard to assess variance as well as random effect validity. <br>",
                                    randomEffectVariable,
                                    dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Count),
                        ModelInsight.ModelInsightSeverity.Warning,
                        randomEffectVariable,
                        dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Count);
                }
                foreach (var randomCovariate in mixedModel.GetRandomLinearFormula(randomEffectVariable).AllVariables.Except(new[] { "0", "1"}))
                {
                    // Missing main effect covariate
                    if (!fixedVars.Contains(randomCovariate))
                    {
                        missingMainEffects.Add(randomCovariate);
                    }

                    if (tableAnalysis.ColumnGraph.ContainsKey(randomEffectVariable) &&
                        (!tableAnalysis.ColumnGraph[randomEffectVariable].ContainsKey(randomCovariate) ||
                        !tableAnalysis.ColumnGraph[randomEffectVariable][randomCovariate].RelationAttributes.Contains(LinkAttributes.StatisticCovering) ||
                         !tableAnalysis.ColumnGraph[randomEffectVariable][randomCovariate].RelationAttributes.Contains(LinkAttributes.StrictCovering)))
                    {
                        nonCoveringCovariates.Add(new Tuple<string, string>(randomCovariate, randomEffectVariable));
                    }
                }
            }

            var cloneModel = mixedModel.Clone();
            foreach (var missingMainEffect in missingMainEffects)
            {
                cloneModel.AndFixedEffect(missingMainEffect);
            }
            if (missingMainEffects.Any())
            {
                if (missingMainEffects.Count == 1)
                {
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.CovariateMissingMainEffect,
                        string.Format("It seems like {0} is included only as a covariate under a grouping random effect. This means that " + 
                                      "in general you assume {0} has no overall linear connection with {1} but there is such a linear " + 
                                      "connection on particular groups. If this was not the assumption you meant, we suggest the following " + 
                                      "new formula: '{2}'<br>",
                                      missingMainEffects.First(),
                                      mixedModel.PredictedVariable,
                                      Question.GetChangeModel(cloneModel.ModelFormula)),
                        ModelInsight.ModelInsightSeverity.Error,
                        missingMainEffects.First());
                }
                else
                {
                    yield return new ModelInsight(
                        ModelInsight.ModelInsightCode.CovariateMissingMainEffect,
                        string.Format("It seems like some variable (e.g. {0}) are included only as a covariate under a grouping random effect. This means that " +
                                    "in general you assume there covariates has no overall linear connection with {1} but there is such a linear " +
                                    "connection on particular groups. If this was not the assumption you meant, we suggest the following " +
                                    "new formula: '{2}'<br>",
                                    missingMainEffects.First(),
                                    mixedModel.PredictedVariable,
                                    Question.GetChangeModel(cloneModel.ModelFormula)),
                        ModelInsight.ModelInsightSeverity.Error,
                        missingMainEffects);
                    
                }
            }
            if (nonCoveringCovariates.Any())
            {
                yield return new ModelInsight(
                    ModelInsight.ModelInsightCode.CovariateNotCovering,
                    string.Format("It seems like {0} is included only as a covariate under a random effect {1}. From analyzing the data " +
                                  "it seems that values of {0} differ greatly between different value groups of {1}. Since the behaviour " +
                                  "of {0} is so different under different groups it will be hard to test linearity or to test the main effect. " +
                                  "Such a data a pattern is less likely, consider removing the {0}.{2}<br>",
                                  nonCoveringCovariates.First().Item1,
                                  nonCoveringCovariates.First().Item2,
                                  nonCoveringCovariates.Count > 1 ? 
                                    string.Format(" The same also applies to covariates: ({0})", 
                                                  string.Join(", ", nonCoveringCovariates.Skip(1).Select(e => e.Item1))) :
                                    string.Empty),
                    ModelInsight.ModelInsightSeverity.Error,
                    nonCoveringCovariates);
            }

            if (!nestingDetected && !balanceDetected)
            {
                yield return new ModelInsight(
                    ModelInsight.ModelInsightCode.None,
                    "No nesting/balance structure detected.<br>",
                    ModelInsight.ModelInsightSeverity.Notice);
            }
            else if (!nestingDetected)
            {
                yield return new ModelInsight(
                    ModelInsight.ModelInsightCode.None,
                    "No nesting structure detected.<br>",
                    ModelInsight.ModelInsightSeverity.Notice);
            }
        }

        private static string CreateModelExplains(
            MixedLinearModel mixedModel,
            ModelDataset dataset,
            List<ModelInsight> modelInsights)
        {
            var variableExplains = new List<string>();

            Action<StringBuilder, string, string, List<String>, bool> 
                handleRandomSlope = (sb,
                                     randomEffectVariable,
                                     sampleRandomSlope,
                                     formulaVariables,
                                     isFirstRandomGrouping) =>
            {
                sb.AppendFormat(formulaVariables.Count > 2 ?
                    "Several random slopes have been added (e.g. {0}). " :
                    "{0} has been added as a random slope. ",
                sampleRandomSlope);
                sb.AppendFormat("For {1} to be considered as a random slope under {0} grouping, the assumption is that there ",
                                randomEffectVariable,
                                sampleRandomSlope);
                sb.AppendFormat("is a linear connection between {1} and {0}. ", mixedModel.PredictedVariable, sampleRandomSlope);
                sb.AppendFormat("But, it also requires for the slope to vary for different values of {0}. ", randomEffectVariable);
                sb.AppendFormat("In this case, it requires that, for example, for {0} ({2}) the value of {3} per {4} " +
                                "unit is diffrent than the value of {3} per {4} for {1} ({2}). ",
                                dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.ElementAt(0),
                                dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.ElementAt(1),
                                randomEffectVariable,
                                mixedModel.PredictedVariable,
                                sampleRandomSlope);
                sb.AppendFormat("<br><br>The slope variation is assumed to be distributed normally, thus generalizing variation ");
                sb.AppendFormat("to account for unknown values of {0}. ", randomEffectVariable);

                if (isFirstRandomGrouping)
                {
                    sb.AppendFormat("<br><br>A indication for the need of random slope is sometimes given when the model without ");
                    sb.AppendFormat("the random slope display hetroscadicity (a term for unequal variances). ");
                    sb.AppendFormat("This is true since random slope of {0} is ", sampleRandomSlope);
                    sb.AppendFormat("different for example between {0} and {1} ({2}), then the bigger the value of {0} ",
                        dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.ElementAt(0),
                        dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.ElementAt(1),
                        randomEffectVariable);
                    sb.AppendFormat("the higher the variance is expected to be.");
                }
            };

            var isFirstWarning = true;
            Action<StringBuilder, List<ModelInsight>> handleWarning = (sb, modelInsight) =>
            {
                if (modelInsight.Any())
                {
                    sb.AppendFormat("{1}<li style='color:red'>{0}</li> ",
                                    modelInsight.First().InsightText,
                                    isFirstWarning ? "<br><br>" : string.Empty);

                    isFirstWarning = false;
                }
            };

            Action<StringBuilder, string, string, bool> hadnleCovariateCorrelation = 
                (sb, randomEffectVariable, sampleRandomSlope, hasCorrelation) =>
            {
                sb.AppendFormat("<br><br>Also, in this case, the formula represents that there is {0} correlation between ", hasCorrelation ? "a" : "no");
                sb.AppendFormat("the random intercept (the \"1\" constant) and the the random slope for {0}. ", sampleRandomSlope);
                sb.AppendFormat("<br><br>The statistical significance of this correlation variable {0} tested with chi-square ", hasCorrelation ? "is" : "could");
                sb.AppendFormat("test, but even significant results might sometimes be artificial when the ");
                sb.AppendFormat("values of {0} are not centered. ", sampleRandomSlope);

                if (hasCorrelation)
                {
                    sb.AppendFormat("It is advisable to center these values before testing such correlalation");
                }
                else
                {
                    sb.AppendFormat("If correlation was not previously checked, it is advisable to check it "); 
                    sb.AppendFormat("after centering {0} values", sampleRandomSlope);
                }

                handleWarning(sb, modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.CovariateMissingMainEffect &&
                                                           (string) i.InsightParameters[0] == sampleRandomSlope).ToList());
                handleWarning(sb, modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.CovariateNotCovering &&
                                                           ((HashSet<Tuple<string, string>>)i.InsightParameters[0]).Select(t => t.Item1)
                                                                                                                   .Contains(sampleRandomSlope)).ToList());
            };

            Action<StringBuilder, string, bool> handleBasicRandomGrouping = (sb, randomEffectVariable, isFirstRandomGrouping) =>
            {
                sb.AppendFormat("For {0} to be considered a random variable it usually means that it was sampled ", randomEffectVariable);
                sb.AppendFormat("from a larger population. This means that, most likely, present values of {0} ", randomEffectVariable);
                sb.AppendFormat("(e.g. {1}) do not cover all possible values for {0}. <br><br>Selecting {0} to be random would ",
                                randomEffectVariable,
                                string.Join(", ", dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.Take(2)));
                sb.AppendFormat("create a model that generalize for {0} values that are not present by assuming ", randomEffectVariable);
                sb.AppendFormat("that {0} effect on {1} is ditributed normally. ", randomEffectVariable, mixedModel.PredictedVariable);
                sb.AppendFormat("Another way to look at it, is that values of {0} are somewhat correlated given ", mixedModel.PredictedVariable);
                sb.AppendFormat("that they have the same value of {0}. This means that we have reason to beleive ", randomEffectVariable);
                sb.AppendFormat("that, for example, all values for {0} ({1}) are more correlated that values for ",
                                dataset.TableStats.ColumnStats[randomEffectVariable].ValuesCount.Keys.First(),
                                randomEffectVariable);
                sb.AppendFormat("a different {0}.<br><br>For this assumption to be relevant we need to have ", randomEffectVariable);
                sb.AppendFormat("<b>Repeated Measures</b> (i.e values of {0} occur more than once). ", randomEffectVariable);

                if (isFirstRandomGrouping)
                {
                    sb.AppendFormat("In general, a random variable is very common in the following scenarios: ");
                    sb.AppendFormat("A subject (human or other) variable in a desgined experiments where some property was measured ");
                    sb.AppendFormat("more than once or a longitudal study where the subject is tested several times over time. ");
                    sb.AppendFormat("A stimulus variable representing a stimulus chosen from a wide variety of stimulus options ");
                    sb.AppendFormat("(e.g. sentence, film, multi-variate drug combination).");
                }
            };

            var isFirstRandomEffect = true;
            var isFirstRandomSlope = true;
            foreach (var randomEffectVariable in mixedModel.RandomEffectVariables)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("<h2>{0} as a random effect</h2>", randomEffectVariable);
                var formulas = mixedModel.GetRandomLinearFormulas(randomEffectVariable).ToList();

                List<string> formulaVariables = formulas.SelectMany(f => f.AllVariables).ToList();
                if (formulaVariables.Contains("1"))
                {
                    handleBasicRandomGrouping(sb, randomEffectVariable, isFirstRandomEffect);

                    handleWarning(
                        sb,
                        modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.RandomVarNotRepeated &&
                                                    (string)i.InsightParameters.First() == randomEffectVariable).ToList());

                    handleWarning(
                        sb,
                        modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.RandomVarFewLevels &&
                                                    (string)i.InsightParameters.First() == randomEffectVariable).ToList());

                    var nestedWarns =
                        modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.NestedVar &&
                                                    ((List<string>) i.InsightParameters[1]).Contains(randomEffectVariable)).ToList();
                    if (nestedWarns.Any())
                    {
                        sb.AppendFormat("<br><br>Sometimes, the only way to include a variable's effect is as a random effect. ");
                        sb.AppendFormat("This happens when there is a nesting structure in the data. For example if we ");
                        sb.AppendFormat("have an experiments with repeated measures for each subject and we want to include ");
                        sb.AppendFormat("one of the subject traits (e.g. gender) as a fixed effect we will not be able ");
                        sb.AppendFormat("to include subject as a fixed effect since it would render the trait's effect irrelevant. ");
                        foreach (var modelInsight in nestedWarns)
                        {
                            handleWarning(sb, new List<ModelInsight>{modelInsight});
                        }
                    }

                    if (formulaVariables.Count > 1 && formulas.Count == 1) // correlation between random intercept and random slope
                    {
                        var sampleRandomSlope = formulaVariables.First(x => x != "1");
                        sb.Append("<br><br>");
                        handleRandomSlope(sb, randomEffectVariable, sampleRandomSlope, formulaVariables, isFirstRandomSlope);
                        hadnleCovariateCorrelation(sb, randomEffectVariable, sampleRandomSlope, true);
                        isFirstRandomSlope = false;
                    }
                    else if (formulas.Count > 1)
                    {
                        var sampleRandomSlope = formulaVariables.First(x => x != "1");
                        sb.Append("<br><br>");
                        hadnleCovariateCorrelation(sb, randomEffectVariable, sampleRandomSlope, false);
                    }
                }
                else // the "0+var" case
                {
                    var sampleRandomSlope = formulaVariables.First(x => x != "1");
                    handleRandomSlope(sb, randomEffectVariable, sampleRandomSlope, formulaVariables, isFirstRandomSlope);
                    isFirstRandomSlope = false;
                    if (formulaVariables.Count > 1) // correlation between random intercept and random slope
                    {
                        hadnleCovariateCorrelation(sb, randomEffectVariable, sampleRandomSlope, true);
                    }

                    handleWarning(
                        sb,
                        modelInsights.Where(i => i.InsightCode == ModelInsight.ModelInsightCode.RandomVarWithoutContant &&
                                                    (string) i.InsightParameters[0] == randomEffectVariable).ToList());
                }

                // v embed random effect count check
                // v embed repeated measures check
                // v embed examples for random groupings
                // random slope
                //   v basic explain
                //   v hetroscadicity explain
                //   v warn of artificial correlation and suggest fix
                //   v explain de-correlction
                //   v embed no fixed effect for covariate
                //   - warn missing covariate fixed
                //   - warn covariate not covering

                // merge all warnings.

                variableExplains.Add(sb.ToString());
                isFirstRandomEffect = false;
            }

            return string.Join(string.Empty, variableExplains);
        }

        public static Dictionary<string, string> TranslateModel(MixedLinearModel mixedModel, ModelDataset dataset)
        {
            var sections = new Dictionary<string, string>();

            sections["Summary"] = CreateModelSummary(mixedModel, dataset);

            var modelInsights = new List<ModelInsight>();
            try
            {
                modelInsights = CreateModelInsights(mixedModel, dataset).ToList();
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception) // Skip insights when model cannot be analyzed
            {
            }

            Func<ModelInsight.ModelInsightSeverity, string> warningStyle = severity =>
            {
                switch (severity)
                {
                    case ModelInsight.ModelInsightSeverity.Notice:
                        return string.Empty;
                    case ModelInsight.ModelInsightSeverity.Warning:
                        return "style='color:rgb(180,180,0)'";
                    case ModelInsight.ModelInsightSeverity.Error:
                        return "style='color:red'";
                }

                throw new Exception("Unexpected severity!");
            };

            sections["Model Insights"] =
                string.Format("<ul>{0}</ul>",
                    string.Join(string.Empty,
                        modelInsights.Select(e => 
                                             string.Format("<li {1}>{0}</li><br>",
                                                           e.InsightText,
                                                           warningStyle(e.InsightSeverity)))));

            if (mixedModel.RandomEffectVariables.Any())
            {
                sections["Model Intent"] = CreateModelExplains(mixedModel, dataset, modelInsights);
            }
            else
            {
                sections["Model Intent"] = string.Empty;
            }
            return sections;
        }
    }
}
