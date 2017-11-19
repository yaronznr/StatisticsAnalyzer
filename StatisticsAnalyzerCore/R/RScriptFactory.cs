using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.R
{
    public static class RScriptFactory
    {
        public static string WrapVariable(string variable)
        {
            return string.Format("__{0}__", variable);
        }

        private static string WrapFormulaVariables(string formula, MixedLinearModel model)
        {
            int i = 1000000;
            var d = new Dictionary<string, string>();
            foreach (var vr in model.AllVariables.OrderByDescending(e => e.Length))
            {
                var tmpReplace = string.Format("var{0}", i++);
                formula = formula.Replace(vr, tmpReplace);
                d[tmpReplace] = WrapVariable(vr);
            }

            foreach (var kvp in d)
            {
                formula = formula.Replace(kvp.Key, kvp.Value);
            }

            return formula;
        }

        private static string FactorizeGroupingVariables(IEnumerable<string> variables, ModelDataset dataset)
        {
            var sb = new StringBuilder();

            foreach (var v in variables.Distinct())
            {
                if (dataset.TableStats.TableAnalysis.ColumnClassifications.ContainsKey(v) &&
                    dataset.TableStats.TableAnalysis.ColumnClassifications[v] == ColumnClassification.Grouping)
                {
                    sb.AppendLine(string.Format("lst${0} = factor(lst${0})", WrapVariable(v)));                    
                }
            }

            return sb.ToString();
        }

        private static string CreatePostHocPart(MixedLinearModel model, ModelDataset dataset, string modelName)
        {
            var sb = new StringBuilder();

            foreach (var v in model.FixedEffectVariables)
            {
                if (dataset.TableStats.TableAnalysis.ColumnClassifications.ContainsKey(v) &&
                    dataset.TableStats.TableAnalysis.ColumnClassifications[v] == ColumnClassification.Grouping &&
                    dataset.TableStats.ColumnStats[v].ValuesCount.Count > 2)
                {
                    //sb.AppendLine(string.Format("summary(glht({0}, linfct = mcp({1} = \"Tukey\")))", modelName, WrapVariable(v)));
                }
            }

            //sb.AppendLine("confint(model1)");
            return sb.ToString();
        }

        private static string CreateBreuschPaganTestScriptPart(MixedLinearModel model, DataTable dataTable, string modelName)
        {
            if (!model.RandomEffectVariables.Any() &&
                model.GetFixedLinearFormula().VariableGroups.Any(g => g.Any(v => dataTable.Columns[v].DataType != typeof(string))))
            {
                var contvars = model.GetFixedLinearFormula().VariableGroups
                                                            .Where(g => g.Any(v => dataTable.Columns[v].DataType != typeof(string)))
                                                            .Select(g => g.First(v => dataTable.Columns[v].DataType != typeof(string)))
                                                            .Distinct();

                return string.Format("ncvTest({0}, ~{1}, data=lst)", modelName, string.Join("+", contvars.Select(WrapVariable))) + 
                       Environment.NewLine;
            }
            
            return string.Empty;
        }

        private static string CreateLeveneTestScriptPart(MixedLinearModel model, DataTable dataTable, string modelName)
        {
            var sb = new StringBuilder();
            foreach (var variableGroup in model.GetFixedLinearFormula().VariableGroups)
            {
                var values = variableGroup as IList<string> ?? variableGroup.ToList();
                var varGroup = values.Where(e => dataTable.Columns[e].DataType == typeof(string)).ToList();

                if (!varGroup.Any()) continue;  // No factor variables in formula

                var d = new Dictionary<ValueGroupIndex, int>();
                var lastGroup = 0;
                var list = new List<int>();
                foreach (DataRow row in dataTable.Rows)
                {
                    DataRow row1 = row;
                    var valGroup = new ValueGroupIndex(varGroup.Select(vr => row1[vr].ToString()));

                    if (!d.ContainsKey(valGroup))
                    {
                        d[valGroup] = lastGroup++;                    
                    }

                    list.Add(d[valGroup]);
                }

                sb.AppendLine(string.Format("groups = interaction({0})", 
                              string.Join(",", varGroup.Select(vr => string.Format("lst${0}", WrapVariable(vr))))));
                sb.AppendLine(string.Format("modlevene.test(resid({0}), as.factor(groups)) # {1}", modelName, string.Join("_", values)));
            }
            return sb.ToString();
        }

        private static string CreateGLmerScriptPart(MixedLinearModel model, string modelName)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("{1} = glmer(formula = {0}, data = lst, family=binomial, control = glmerControl(optimizer = \"bobyqa\"), nAGQ = 10)", WrapFormulaVariables(model.ModelFormula, model), modelName));
            sb.AppendLine(string.Format("{0}", modelName));
            sb.AppendLine(string.Format("summary({0})", modelName));
            sb.AppendLine(string.Format("ranef({0})", modelName));
            return sb.ToString();
        }

        private static string CreateLinearLmerScriptPart(MixedLinearModel model, string modelName)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("{1} = lmer(formula = {0}, data = lst, REML=TRUE)", WrapFormulaVariables(model.ModelFormula, model), modelName));
            sb.AppendLine(string.Format("{0}", modelName));
            sb.AppendLine(string.Format("summary({0})", modelName));
            sb.AppendLine(string.Format("ranef({0})", modelName));
            sb.AppendLine("result = c()");
            sb.AppendLine(string.Format(
                "tryCatch({{" +
                string.Format("result = shapiro.test(ranef({0})[[1]][[1]])", modelName) +
                @"}}, warning = function(w) {{ result = c() }}, error = function(e) {{ result = c() }}, finally = {{}})", modelName));
            sb.AppendLine("result");  // TODO: in case previous line fails this will display previous R result variable

            return sb.ToString();
        }

        private static string CreateLmerScriptPart(ModelDataset dataset, MixedLinearModel model, string modelName)
        {
            if (dataset.DataTable.Columns[model.PredictedVariable].DataType == typeof(string))
            {
                return CreateGLmerScriptPart(model, modelName);
            }
            else
            {
                return CreateLinearLmerScriptPart(model, modelName);
            }
        }

        private static string CreateLoadLibrariesPart()
        {
            var sb = new StringBuilder();
            sb.AppendLine("library(\"lme4\")");
            sb.AppendLine("library(\"XLConnect\")");
            sb.AppendLine("library(\"car\")");
            sb.AppendLine("library(\"glmnet\")");
            sb.AppendLine("library(\"asbio\")");
            sb.AppendLine("library(\"pbkrtest\")");
            sb.AppendLine("library(\"multcomp\")");
            sb.AppendLine("library(\"influence.ME\")");
            sb.AppendLine("library(\"ResourceSelection\")");
            return sb.ToString();
        }

        private static string CreateLoadExcelPart()
        {
            var sb = new StringBuilder();
            sb.AppendLine("wb <- loadWorkbook(\"{file}\")");
            sb.AppendLine("lst = readWorksheet(wb, sheet = getSheets(wb))");
            return sb.ToString();
        }

        private static string CreateRegressionScriptPart(ModelDataset dataset, MixedLinearModel model, string modelName)
        {
            if (dataset.DataTable.Columns[model.PredictedVariable].DataType == typeof(string))
            {
                return string.Format("{0} = glm({1}, data = lst, family=binomial)", modelName, WrapFormulaVariables(model.ModelFormula, model));
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Format("{0} = lm({1}, data = lst)", modelName, WrapFormulaVariables(model.ModelFormula, model)));
                if (!model.ModelFormula.Contains("*"))
                {
                    string glmnetFormula = string.Format("cv.glmnet(as.matrix(lst[,c({0})]), lst${1}, alpha = 1.0)",
                        WrapFormulaVariables(string.Join(",", model.FixedEffectVariables.Select(x => string.Format("\"{0}\"", x)).ToArray()), model),
                        WrapVariable(model.PredictedVariable));
                    sb.AppendLine(string.Format("coef({0}, s = \"lambda.min\")", glmnetFormula));                    
                }
                return sb.ToString();
            }
        }

        private static string CreateMixedModelTestsPart(ModelDataset dataset, MixedLinearModel model)
        {
            var sb = new StringBuilder();

            int i = 2;
            if (model.RandomEffectVariables.Count() > 1)
            {
                foreach (var randomEffect in model.RandomEffectVariables)
                {
                    var modelName = string.Format("model{0}", i++);
                    var clone = model.Clone();
                    clone.RemoveRandomEffectPart(randomEffect);
                    sb.AppendLine(CreateLmerScriptPart(dataset, clone, modelName));
                    sb.AppendLine(string.Format("anova(model1,{0})", modelName));
                }
            }
            else
            {
                var clone = model.Clone();
                clone.RemoveRandomEffectPart(model.RandomEffectVariables.Single());
                sb.AppendLine(CreateRegressionScriptPart(dataset, clone, string.Format("model{0}", i++)));
                sb.AppendLine("chisq_value = (2*(logLik(model1,REML=FALSE)-logLik(model2,REML=FALSE)))");
                sb.AppendLine("chisq_value");
                sb.AppendLine("pchisq(chisq_value,1,lower.tail=FALSE)");
            }

            if (model.RandomEffectVariables.Any())
            {
                foreach (var rndEffect in model.RandomEffectVariables)
                {
                    var rndEffectFormulas = model.GetRandomLinearFormulas(rndEffect);
                    foreach (var rndEffectFormula in rndEffectFormulas)
                    {
                        if (rndEffectFormula.VariableGroups.Count > 1)
                        {
                            foreach (var randomEffectRegressor in rndEffectFormula.AllVariables.Except(new[] { "0", "1" }))
                            {
                                var modelName = string.Format("model{0}", i++);
                                var clone = model.Clone();
                                clone.RemoveRandomEffectFormulaVariable(rndEffect, randomEffectRegressor);
                                sb.AppendLine(CreateLmerScriptPart(dataset, clone, modelName));
                                sb.AppendLine(string.Format("anova(model1,{0})", modelName));
                            }
                        }                        
                    }
                }
            }

            return sb.ToString();
        }

        private static string CreateInfluenceAnalysis(MixedLinearModel model, ModelDataset dataset, string modelName)
        {
            var sb = new StringBuilder();

            if (model.RandomEffectVariables.Any())
            {
                foreach (var randomEffect in model.RandomEffectVariables)
                {
                    sb.AppendLine(string.Format("inf = influence({0}, group=\"{1}\")", modelName, WrapVariable(randomEffect)));
                    sb.AppendLine(string.Format("cooks.distance(inf) # {0}", WrapVariable(randomEffect)));
                }

                if (dataset.DataTable.Rows.Count < 1000)
                {
                    sb.AppendLine(string.Format("inf = influence({0}, obs=TRUE)", modelName));
                    sb.AppendLine("tail(sort(cooks.distance(inf)),20)");
                    sb.AppendLine(string.Format("order(cooks.distance(inf))[{0}:{1}]", 
                                                dataset.DataTable.Rows.Count-19,
                                                dataset.DataTable.Rows.Count));
                }
            }
            else
            {
                sb.AppendLine(string.Format("inf = lm.influence({0})", modelName));
                sb.AppendLine(string.Format("tail(sort(cooks.distance({0}, inf)),20)", modelName));
            }

            return sb.ToString();
        }

        private static IEnumerable<string> CreateProcessGeneralBinomialMixedModelScriptPart(
            MixedLinearModel model,
            ModelDataset dataset,
            bool sparseScript,
            List<int> contrastList)
        {
            var dataTable = dataset.DataTable;

            if (model.RandomEffectVariables.Any())
            {
                if (sparseScript)
                {
                    yield return string.Format("model1 = glmer(formula = {0}, data = lst, family=binomial, control = glmerControl(optimizer = \"bobyqa\"), nAGQ = 10)", WrapFormulaVariables(model.ModelFormula, model));
                }
                else
                {
                    yield return CreateGLmerScriptPart(model, "model1");
                    yield return CreateMixedModelTestsPart(dataset, model);
                }
            }
            else
            {
                yield return string.Format("model1 = glm(formula = {0}, data = lst, family=binomial)", WrapFormulaVariables(model.ModelFormula, model));
            }

            yield return "summary(model1)";

            yield return string.Format("lst${0}", WrapVariable(model.PredictedVariable));
            yield return string.Format("fitted(model1)", WrapVariable(model.PredictedVariable));
            yield return string.Format("hl <- hoslem.test(na.omit(lst${0}), fitted(model1))", WrapVariable(model.PredictedVariable));
            yield return string.Format("hl");
            //yield return "Anova(model1, type = 3, contrasts=list(topic=contr.sum, sys=contr.sum), test.statistic = 'F')";
            /*if (!sparseScript)
            {
                yield return "shapiro.test(residuals(model1)[1:5000])";
                if (model.RandomEffectVariables.Any())
                {
                    yield return "durbinWatsonTest(resid(model1), max.lag=1)";
                }
                else
                {
                    yield return "durbinWatsonTest(model1$residuals, max.lag=1)";
                }
                yield return CreateLeveneTestScriptPart(model, dataTable, "model1");
                yield return CreateBreuschPaganTestScriptPart(model, dataTable, "model1");
                yield return CreatePostHocPart(model, dataset, "model1");
                //Psb.Append(CreateInfluenceAnalysis(model, dataset, "model1"));
            }*/

            /*if (model.FixedEffectVariables.Count() == 1 &&
                dataTable.Columns[model.FixedEffectVariables.Single()].DataType == typeof(string) &&
                !model.RandomEffectVariables.Any())
            {
                yield return string.Format("t.test(lst${0} ~ lst${1}, var.equal=FALSE)",
                              WrapVariable(model.PredictedVariable),
                              WrapVariable(model.FixedEffectVariables.First()));
            }*/

            yield return "proc.time()";
        }

        private static IEnumerable<string> CreateProcessLinearMixedModelScriptPart(
            MixedLinearModel model,
            ModelDataset dataset,
            bool sparseScript,
            List<int> contrastList)
        {
            var dataTable = dataset.DataTable;

            if (model.RandomEffectVariables.Any())
            {
                if (sparseScript)
                {
                    yield return string.Format("model1 = lmer(formula = {0}, data = lst, REML=TRUE)", WrapFormulaVariables(model.ModelFormula, model));
                }
                else
                {
                    yield return CreateLmerScriptPart(dataset, model, "model1");
                    yield return CreateMixedModelTestsPart(dataset, model);
                }

                foreach (var contrast in contrastList)
                {
                    // Create contrast only when more than one variable is being compared
                    if (contrast < model.FixedEffectVariables.Count() - 1)
                    {
                        yield return string.Format("tryCatch(KRmodcomp(model1,contr.sum({0})[{1}:{2},]), error = function(e) {{}})",
                                                   model.FixedEffectVariables.Count() + 2,
                                                   contrast + 2,
                                                   model.FixedEffectVariables.Count() + 1);
                    }
                }
            }
            else
            {
                yield return CreateRegressionScriptPart(dataset, model, "model1");
            }

            yield return "summary(model1)";
            yield return "Anova(model1, type = 3, contrasts=list(topic=contr.sum, sys=contr.sum), test.statistic = 'F')";
            if (!sparseScript)
            {
                yield return "shapiro.test(residuals(model1)[1:5000])";
                if (model.RandomEffectVariables.Any())
                {
                    yield return "durbinWatsonTest(resid(model1), max.lag=1)";
                }
                else
                {
                    yield return "durbinWatsonTest(model1$residuals, max.lag=1)";
                }
                yield return CreateLeveneTestScriptPart(model, dataTable, "model1");
                yield return CreateBreuschPaganTestScriptPart(model, dataTable, "model1");
                yield return CreatePostHocPart(model, dataset, "model1");
                yield return CreateInfluenceAnalysis(model, dataset, "model1");
            }

            if (model.FixedEffectVariables.Count() == 1 &&
                dataTable.Columns[model.FixedEffectVariables.Single()].DataType == typeof(string) &&
                !model.RandomEffectVariables.Any())
            {
                yield return string.Format("t.test(lst${0} ~ lst${1}, var.equal=FALSE)",
                              WrapVariable(model.PredictedVariable),
                              WrapVariable(model.FixedEffectVariables.First()));
            }

            yield return "proc.time()";
        }

        public static string CreateMixedModelScript(List<MixedLinearModel> models,
                                                    ModelDataset dataset,
                                                    bool sparseScript,
                                                    List<int> contrastList)
        {
            var dataTable = dataset.DataTable;

            var sb = new StringBuilder();

            sb.Append(CreateLoadLibrariesPart());
            sb.Append(CreateLoadExcelPart());
            sb.Append(FactorizeGroupingVariables(models.SelectMany(m => m.AllVariables), dataset));

            foreach (var model in models)
            {
                if (dataTable.Columns[model.PredictedVariable].DataType != typeof(string))
                {
                    foreach (var cmd in CreateProcessLinearMixedModelScriptPart(model, dataset, sparseScript, contrastList))
                    {
                        sb.AppendLine(cmd);
                    }
                }
                else
                {
                    foreach (var cmd in CreateProcessGeneralBinomialMixedModelScriptPart(model, dataset, sparseScript, contrastList))
                    {
                        sb.AppendLine(cmd);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
