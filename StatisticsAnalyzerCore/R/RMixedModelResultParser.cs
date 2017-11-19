using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.R
{
    public static class RMixedModelResultParser
    {
        private class EffectParams
        {
            public string Estimate { get; set; }
            public string StdDev { get; set; }
// ReSharper disable InconsistentNaming
            public string TValue { get; set; }
// ReSharper restore InconsistentNaming
            public string PValue { get; set; }
        }

        private static List<string> BreakRResultToLines(string rCommandResult)
        {
            return
                rCommandResult.Replace("<br>", string.Empty)
                    .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
        }

        private static Dictionary<string, EffectParams> MergeFixedEffectLines(
            IEnumerable<string> modelResultLines,
            bool isBinomial)
        {
            var fixedEffectDictionary = new Dictionary<string, EffectParams>();

            var fixedEffects = modelResultLines.SkipWhile(e => !e.StartsWith("Coefficients:"))
                .TakeWhile(e => !e.StartsWith("---") &&
                                !string.IsNullOrEmpty(e.Trim()) &&
                                !e.StartsWith("Residual standard error:") &&
                                !e.Contains("(Dispersion parameter for binomial family"));

            var expectedParameters = isBinomial ?
                new[] { "Estimate", "Std. Error", "z value", "Pr(>|z|)" } :
                new[] { "Estimate", "Std. Error", "t value", "Pr(>|t|)" };
            int startIndex = 0, paramCount = 0;
            foreach (var line in fixedEffects.Skip(1))
            {
                // New parameter set
                if (line.StartsWith(" "))
                {
                    string paramSetLine = line;
                    var containedParams = expectedParameters.Select(paramSetLine.Contains).ToArray();
                    startIndex = containedParams.TakeWhile(prm => !prm).Count();
                    paramCount = containedParams.Count(prm => prm);
                    continue;
                }

                // Skip intercept for now
                /*if (line.Contains("(Intercept)"))
                {
                    continue;
                }*/

                // Read fixed effect line
                var trimmmedLine = line.TrimEnd(new[] {' '})
                    .TrimEnd(new[] {'*'})
                    .TrimEnd(new[] {'.'})
                    .Replace("<", "")
                    .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                var nameStringCount = trimmmedLine.Length - paramCount;
                var effectName = string.Join(" ", trimmmedLine.Take(nameStringCount));
                var effectParamList = trimmmedLine.Skip(nameStringCount).ToList();

                EffectParams effectParams;
                if (!fixedEffectDictionary.TryGetValue(effectName, out effectParams))
                {
                    effectParams = new EffectParams();
                    fixedEffectDictionary.Add(effectName, effectParams);
                }

                for (var i = 0; i < paramCount; i++)
                {
                    switch (i + startIndex)
                    {
                        case 0:
                            effectParams.Estimate = effectParamList[i];
                            break;
                        case 1:
                            effectParams.StdDev = effectParamList[i];
                            break;
                        case 2:
                            effectParams.TValue = effectParamList[i];
                            break;
                        case 3:
                            effectParams.PValue = effectParamList[i];
                            break;
                    }
                }
            }

            return fixedEffectDictionary;
        }

        private static void RetreiveFixedEffect(string effectName, 
                                                MixedLinearModel model,
                                                out VarGroupIndex varGroupIndex,
                                                out ValueGroupIndex valueGroupIndex)
        {
            var fixedEffectVariables = new HashSet<string>(model.FixedEffectVariables);

            var effectComponents = effectName.Split(':');
            var fixedEffectGroup = effectComponents[0] == "(Intercept)" ?
                effectComponents :
                effectComponents.Select(cmp => fixedEffectVariables.FirstOrDefault(cmp.StartsWith)).ToArray();
            var fixedEffectValues = effectComponents[0] == "(Intercept)" ?
                effectComponents :
                effectComponents.Select(cmp => cmp.Substring(fixedEffectVariables.First(cmp.StartsWith).Length)).ToArray();

            varGroupIndex = new VarGroupIndex(fixedEffectGroup);
            valueGroupIndex = new ValueGroupIndex(fixedEffectValues);
        }

        private static void ComputeFittedValuesAndResiduals(MixedLinearModel model,
                                                            LinearMixedModelResult result,
                                                            ModelDataset dataset)
        {
            var dataTable = dataset.DataTable;
            var fittedValues = result.GetFittedValues(dataset);

            var errors = new List<double?>();
            for (int i = 0; i < fittedValues.Count; i++)
            {
                var rowVal = dataTable.Rows[i][model.PredictedVariable];
                if (!fittedValues[i].HasValue || rowVal.IsNull())
                {
                    errors.Add(null);
                }
                else
                {
                    errors.Add(rowVal.ConvertDouble()-fittedValues[i].Value);
                }
            }

            result.ResidualStats.FittedValues = fittedValues.ToArray();
            result.ResidualStats.Residuals = errors.ToArray();
        }

        private static double[] ParseDoublesArray(IEnumerable<string> arrayLines)
        {
            var doubleList = new List<double>();
            var skipLine = true;
            foreach (var arrayLine in arrayLines.Skip(1))
            {
                if (!skipLine)
                {
                    doubleList.AddRange(arrayLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse));
                }

                skipLine = !skipLine;
            }

            return doubleList.ToArray();
        }

        private static T[] ParseObjectList<T>(IEnumerable<string> listLines, Func<string, T> converter)
        {
            var objList = new List<T>();
            foreach (var arrayLine in listLines.Skip(1))
            {
                objList.AddRange(arrayLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(converter));
            }

            return objList.ToArray();            
        }

        private static int[] ParseIndicesArray(IEnumerable<string> arrayLines)
        {
            var intList = new List<int>();
            var skipLine = false;
            foreach (var arrayLine in arrayLines.Skip(1))
            {
                if (!skipLine)
                {
                    intList.AddRange(arrayLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse));
                }

                skipLine = !skipLine;
            }

            return intList.ToArray();
        }

        private static ResidualsTest ParseResidualsTest(List<string> residSummaryLines)
        {
            return new ResidualsTest
            {
                WValue = double.Parse(residSummaryLines[3].Split(',')[0].Split('=', '<')[1]),
                PValue = double.Parse(residSummaryLines[3].Split(',')[1].Split('=', '<')[1]),
            };
        }

        private static void ParseConfidenceInterval(MixedLinearModel model, string confidenceRaw, LinearMixedModelResult result)
        {
            var res = BreakRResultToLines(confidenceRaw);

            foreach (var confLine in res.Skip(3))
            {
                if (confLine.StartsWith(".")) // Var confs
                {
                    
                }
                else
                {
                    var parts = confLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                    VarGroupIndex varGroupIndex;
                    ValueGroupIndex valueGroupIndex;
                    RetreiveFixedEffect(parts[0], model, out varGroupIndex, out valueGroupIndex);
                    if (result.FixedEffectResults.ContainsKey(varGroupIndex) &&
                        result.FixedEffectResults[varGroupIndex].EffectResults.ContainsKey(valueGroupIndex))
                    {
                        result.FixedEffectResults[varGroupIndex].EffectResults[valueGroupIndex].ConfidenceInterval =
                            new ConfidenceInterval
                            {
                                Low = double.Parse(parts[1]),
                                High = double.Parse(parts[2]),
                            };
                    }
                }
                
            }
        }

        private static Dictionary<VarGroupIndex, AnovaResult> ParseAnovaType3(List<string> anovaResultLines)
        {
            var retDict = new Dictionary<VarGroupIndex, AnovaResult>();

            var anovaResults = anovaResultLines.SkipWhile(e => !e.StartsWith("(Intercept)"))
                                               .TakeWhile(e => !e.StartsWith("---"))
                                               .ToList();

            var isKenwardRogerPlot = anovaResultLines.Any(line => line.Contains("Kenward-Roger"));
            var anovaResidDf = 0.0;
            if (!isKenwardRogerPlot)
            {
                anovaResidDf = double.Parse(anovaResults.First(e => e.StartsWith("Residuals")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[2]);
            }

            foreach (var anovaResult in anovaResults)
            {
                var resultParams = anovaResult.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (resultParams[0] != "Residuals" && resultParams[0] != "(Intercept)")
                    retDict.Add(new VarGroupIndex(resultParams[0].Split(':')),
                                new AnovaResult
                                {
                                    FValue = double.Parse(resultParams[isKenwardRogerPlot ? 1: 3]),
                                    DegreeFreedom = int.Parse(resultParams[2]),
                                    DegreeFreedomRes = isKenwardRogerPlot ? double.Parse(resultParams[3]) : anovaResidDf,
                                    PValue = double.Parse(resultParams[4] == "<" ? resultParams[5] : resultParams[4].Replace("<", "")),
                                    SumSquare = isKenwardRogerPlot ? -1 : double.Parse(resultParams[1]),
                                });
            }

            return retDict;
        }

        private static Dictionary<VarGroupIndex, LeveneTest> ParseLeveneTests(IEnumerable<string> leveneTestsRaw)
        {
            var d = new Dictionary<VarGroupIndex, LeveneTest>();
            foreach (var leveneTestCmdRaw in leveneTestsRaw)
            {
                var leveneLines = BreakRResultToLines(leveneTestCmdRaw);
                var varGroupIndex = new VarGroupIndex(leveneLines[0].Split('#')[1]
                                                                    .Split('_')
                                                                    .Select(v => v.Trim()));
                d[varGroupIndex] = leveneLines
                    .Where(l => l.StartsWith("df1"))
                    .Select(line =>
                    {
                        var res = line.Replace("Warning messages:", "");
                        var parts = res.Split(',').ToList();
                        return new LeveneTest
                        {
                            Df1 = double.Parse(parts[0].Split('=')[1]),
                            Df2 = double.Parse(parts[1].Split('=')[1]),
                            FValue = double.Parse(parts[2].Split('=')[1]),
                            PValue = double.Parse(parts[3].Split('=')[1]),

                        };
                    })
                    .First();
            }

            return d;
        }

        private static BreuschPaganTest ParseBreuschPaganTests(string breuschPaganTestsRaw)
        {
            if (!string.IsNullOrEmpty(breuschPaganTestsRaw))
            {
                var breuschPaganLines = BreakRResultToLines(breuschPaganTestsRaw);
                var line = breuschPaganLines.Skip(3).First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                return new BreuschPaganTest
                {
                    ChiSquare = double.Parse(line[2]),
                    Df = double.Parse(line[5]),
                    PValue = double.Parse(line[8]),
                };                
            }

            return null;
        }

        private static CookDistances ParseCookDistances(List<string> influenceRaw, ModelDataset dataset, MixedLinearModel model)
        {
            var d = new CookDistances
            {
                ObsCookDistances = new Dictionary<int, double>(),
                RandomCookDistances = new Dictionary<string, Dictionary<string, double>>(),
            };

            foreach (var influence in influenceRaw)
            {
                var infLines = BreakRResultToLines(influence);
                if (infLines[0].Contains("#"))
                {
                    var randomEffect = infLines[0].Split('#')[1].Trim();
                    d.RandomCookDistances[randomEffect] = 
                        infLines.Skip(2)
                                .Take(dataset.TableStats.ColumnStats[randomEffect].ValuesCount.Count)
                                .ToDictionary(e =>
                                              {
                                                  var p = e.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
                                                  return p.Length == 1 ? string.Empty : p[0];
                                              },
                                              e => 
                                              {
                                                  var p = e.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
                                                  return p.Length == 1 ? double.Parse(p[0]) : double.Parse(p[1]);
                                              });
                }

                if (infLines[0].Contains("tail(sort"))
                {
                    double[] vals;
                    int[] inds;
                    if (model.RandomEffectVariables.Any())
                    {
                        var indLines = BreakRResultToLines(influenceRaw.First(e => e.StartsWith("order(cooks.distance")));
                        vals = ParseObjectList(infLines, double.Parse);
                        inds = ParseObjectList(indLines, int.Parse);
                    }
                    else
                    {
                        vals = ParseDoublesArray(infLines);
                        inds = ParseIndicesArray(infLines);
                    }

                    for (int i = 0; i < inds.Length; i++)
                    {
                        d.ObsCookDistances[inds[i]] = vals[i];
                    }
                }
            }

            return d;
        }

        private static UnequalVarianceTTest ParseUnequalVarianceTTest(string unequalTTestRaw)
        {
            if (unequalTTestRaw == null) return null;

            var resultLines = BreakRResultToLines(unequalTTestRaw);
            var resultLine = resultLines.First(l => l.StartsWith("t ="))
                                        .Replace("<", "=")
                                        .Split(',');

            return new UnequalVarianceTTest
            {
                T = double.Parse(resultLine[0].Split('=')[1]),
                Df = double.Parse(resultLine[1].Split('=')[1]),
                PValue = double.Parse(resultLine[2].Split('=')[1]),
            };
        }

        private static Dictionary<List<int>, AnovaResult> ParseContrastsTest(List<string> constrastsTestsRaw)
        {
            var d = new Dictionary<List<int>, AnovaResult>();

            foreach (var contrast in constrastsTestsRaw)
            {
                // contrasts computation failed
                if (!contrast.Contains("F.scaling")) continue;

                var lines = BreakRResultToLines(contrast);

                List<int> cont = new List<int>();
                foreach (var coefLine in lines.Where(line => line.Contains(",]")))
                {
                    cont.Add(coefLine.Split(new[] { " 1 " }, StringSplitOptions.RemoveEmptyEntries)[0].Count(ch => ch == '.'));
                }

                var anovaLine = lines.SkipWhile(line => !line.Contains("F.scaling"))
                                        .Skip(1)
                                        .First()
                                        .Replace("<", string.Empty)
                                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                d[cont] = new AnovaResult
                {
                    DegreeFreedom = (int)double.Parse(anovaLine[2]),
                    DegreeFreedomRes = double.Parse(anovaLine[3]),
                    FValue = double.Parse(anovaLine[1]),
                    PValue = double.Parse(anovaLine[5]),
                    SumSquare = -1,
                };
            }

            return d;
        }

        private static BinomialFitResult ParseFitResultGlm(IEnumerable<string> modelResultLines)
        {
            var fitLine = modelResultLines.SkipWhile(line => !line.Contains("Control: glmerControl"))
                                          .Skip(2)
                                          .First()
                                          .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return new BinomialFitResult
            {
                Aic = double.Parse(fitLine[0]),
                Bic = double.Parse(fitLine[1]),
                LogLikelihood = double.Parse(fitLine[2]),
                Deviance = double.Parse(fitLine[3]),
                UsedDf = int.Parse(fitLine[4]),
                DatasetDf = 300,
                PredictValue = 0.0,
                Roc = 0.0,
            };
        }

        private static FitResult ParseFitResultLm(IEnumerable<string> modelResultLines)
        {
            var fStatisticLine = modelResultLines.First(line => line.Contains("F-statistic:")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return new FitResult
            {
                DatasetDf = int.Parse(fStatisticLine[5]),
                UsedDf = int.Parse(fStatisticLine[3]),
                FValue = double.Parse(fStatisticLine[1]),
                PValue = double.Parse((fStatisticLine[8] == "<") ? fStatisticLine[9] : fStatisticLine[8]),
            };
        }
        private static ResidualStats ParseResidualStatsLm(List<string> modelResultLines,
                                                          List<string> residResultLines)
        {
            var stats = new ResidualStatsLm();

            var quadLines = modelResultLines.SkipWhile(line => !line.StartsWith("Residuals:"))
                                            .Skip(2)
                                            .First()
                                            .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            stats.Min = double.Parse(quadLines[0]);
            stats.Q1  = double.Parse(quadLines[1]);
            stats.Q2  = double.Parse(quadLines[2]);
            stats.Q3  = double.Parse(quadLines[3]);
            stats.Max = double.Parse(quadLines[4]);
            stats.ResidualsTest = ParseResidualsTest(residResultLines);

            var residLine = modelResultLines.SkipWhile(line => !line.StartsWith("Residual standard error:"))
                                            .First()
                                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            stats.Std = double.Parse(residLine[3]);
            stats.Df = int.Parse(residLine[5]);
            return stats;
        }
        private static List<string> ParseCvGlmnet(IEnumerable<string> cvGlmnetLines)
        {
            if (cvGlmnetLines == null)
            {
                return null;
            }

            var selectedVars = cvGlmnetLines.SkipWhile(line => !line.Contains("(Intercept)"))
                                            .Skip(1)
                                            .TakeWhile(line => line.Trim().Length > 0)
                                            .Select(line => line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
                                            .Where(varPair => varPair.Length > 1 && varPair[1] != ".")
                                            .Select(varPair => varPair[0])
                                            .ToList();
            return selectedVars;
        }
        private static Dictionary<VarGroupIndex, FixedEffectResult> ParseFixedEffectsLm(MixedLinearModel model, 
                                                                                        IEnumerable<string> modelResultLines,
                                                                                        ModelDataset dataset)
        {
            var fixedEffects = MergeFixedEffectLines(
                modelResultLines,
                dataset.DataTable.Columns[model.PredictedVariable].DataType == typeof(string));

            var fixedEffectResults = new Dictionary<VarGroupIndex, FixedEffectResult>();
            foreach (var effect in fixedEffects)
            {
                var effectName = effect.Key;
                var effectParams = effect.Value;

                // Skip missing measurements
                if (effectParams.Estimate.Contains("NA"))
                {
                    continue;
                }

                VarGroupIndex varGroupIndex;
                ValueGroupIndex valueGroupIndex;
                RetreiveFixedEffect(effectName, model, out varGroupIndex, out valueGroupIndex);

                // Parse level effect
                FixedEffectResult fixedLevelEffectResult;
                if (!fixedEffectResults.ContainsKey(varGroupIndex))
                {
                    fixedLevelEffectResult = new FixedEffectResult{ EffectResults = new Dictionary<ValueGroupIndex, SingleLevelEfectResult>() };
                    fixedEffectResults.Add(varGroupIndex, fixedLevelEffectResult);
                }
                fixedLevelEffectResult = fixedEffectResults[varGroupIndex];

                fixedLevelEffectResult.EffectResults.Add(valueGroupIndex,
                                                            new SingleLevelEfectResult
                                                            {
                                                                LevelValue = valueGroupIndex,
                                                                Estimate = double.Parse(effectParams.Estimate),
                                                                StdError = double.Parse(effectParams.StdDev),
                                                                TValue = double.Parse(effectParams.TValue),
                                                                PValue = double.Parse(effectParams.PValue),
                                                            });
            }

            return fixedEffectResults;
        }

        private static Dictionary<VarGroupIndex, FixedEffectResult> ParseFixedEffectsLme(
            MixedLinearModel model,
            IEnumerable<string> modelResultLines,
            DataTable dataTable,
            bool isLinear)
        {
            var fixedEffectVariables = model.FixedEffectVariables.ToList();

            IEnumerable<string> fixedEffects;

            if (dataTable.Columns[model.PredictedVariable].DataType != typeof(string))
            {
                fixedEffects = modelResultLines.SkipWhile(e => !e.StartsWith("Fixed effects:"))
                                               .TakeWhile(e => !e.StartsWith("Correlation of Fixed Effects:") &&
                                                               !e.StartsWith("Correlation matrix not shown by default"))
                                               .Where(e => !string.IsNullOrEmpty(e.Trim()));
            }
            else
            {
                fixedEffects = modelResultLines.SkipWhile(e => !e.StartsWith("Fixed effects:"))
                                               .TakeWhile(e => !e.StartsWith("---") &&
                                                               !e.StartsWith("Correlation of Fixed Effects:"))
                                               .Where(e => !string.IsNullOrEmpty(e.Trim()));
            }

            var effects = fixedEffects.Skip(2)
                                      .Select(e => e.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .ToList()
                                                    .Where(a => a != "<")
                                                    .ToArray());

            var fixedEffectResults = new Dictionary<VarGroupIndex, FixedEffectResult>();
            foreach (var effect in effects)
            {
                if ((model.FixedEffectVariables.Contains(effect[0]) &&
                    dataTable.Columns[effect[0]].DataType != typeof(string)) ||
                    effect[0] == "(Intercept)")
                {
                    fixedEffectResults.Add(new VarGroupIndex(effect[0]),
                                           new FixedEffectResult
                                           {
                                               EffectResults = new Dictionary<ValueGroupIndex, SingleLevelEfectResult>
                                               {
                                                   { 
                                                       new ValueGroupIndex(), 
                                                       new SingleLevelEfectResult
                                                       {
                                                           Estimate = double.Parse(effect[1]),
                                                           StdError = double.Parse(effect[2]),
                                                           TValue = double.Parse(effect[3]),
                                                       }
                                                   },
                                               },
                                           });
                    continue;
                }

                var levelComponentsCount = effect.Length - (effect.Last().StartsWith("*") ? 4 : 3);
                if (!isLinear) levelComponentsCount -= 1; 
                var levelEffectString = string.Join(" ", effect.Take(levelComponentsCount).ToArray());
                var varGroupIndex = levelEffectString.Split(':')
                                                     .Select(v => fixedEffectVariables.First(v.StartsWith))
                                                     .ToList();
                FixedEffectResult fixedLevelEffectResult;
                if (!fixedEffectResults.ContainsKey(new VarGroupIndex(varGroupIndex)))
                {
                    fixedLevelEffectResult = new FixedEffectResult{ EffectResults = new Dictionary<ValueGroupIndex, SingleLevelEfectResult>() };
                    fixedEffectResults.Add(new VarGroupIndex(varGroupIndex), fixedLevelEffectResult);
                }
                fixedLevelEffectResult = fixedEffectResults[new VarGroupIndex(varGroupIndex)];

                var valueGroupIndex = new ValueGroupIndex(levelEffectString.Split(':')
                                        //.Where(v => dataTable.Columns[fixedEffectVariables.First(v.StartsWith)].DataType == typeof(string))
                                        .Select(v => v.Replace(fixedEffectVariables.First(v.StartsWith), string.Empty))
                                        .ToList());
                fixedLevelEffectResult.EffectResults.Add(valueGroupIndex,
                                                            new SingleLevelEfectResult
                                                            {
                                                                LevelValue = valueGroupIndex,
                                                                Estimate = double.Parse(effect[levelComponentsCount]),
                                                                StdError = double.Parse(effect[levelComponentsCount + 1]),
                                                                TValue = double.Parse(effect[levelComponentsCount + 2]),
                                                            });
            }

            return fixedEffectResults;
        }

        private static Dictionary<string, RandomEffectResult> ParseRandomEffectsLme(MixedLinearModel model,
                                                                                    IEnumerable<string> modelResultLines,
                                                                                    IEnumerable<string> ranefResultLines)
        {
            var randomEffectVariables = model.RandomEffectVariables.ToList();

            var randomEffects = modelResultLines.SkipWhile(e => !e.StartsWith("Random effects:"))
                                                .TakeWhile(e => !e.StartsWith(" Residual") && !e.StartsWith("Number of obs:"));

            var effects = randomEffects.Skip(2)
                                       .Select(e => e.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                     .ToList()
                                                     .Where(a => a != "<")
                                                     .ToArray());

            var ranodmEffectResults = new Dictionary<string, RandomEffectResult>();
            string currEffect = null;
            foreach (var effect in effects)
            {
                // Trim ".1" which has no meaning
                if (effect[0].EndsWith(".1"))
                {
                    effect[0] = effect[0].Substring(0, effect[0].Length - 2);
                    ranodmEffectResults[currEffect].RandomEfects.Add(effect[1],
                                                                     new SingleRandomEfectResult
                                                                     {
                                                                         Variance = double.Parse(effect[2]),
                                                                         StdError = double.Parse(effect[3]),
                                                                         RandomEffects = new Dictionary<string, double>(),
                                                                     });
                    continue;
                }

                var effectParams = effect;

                // If encountered new random variable
                if (randomEffectVariables.Contains(effect[0]) && !ranodmEffectResults.ContainsKey(effect[0]))
                {
                    ranodmEffectResults.Add(effect[0], new RandomEffectResult());
                    ranodmEffectResults[effect[0]].RandomEfects = new Dictionary<string, SingleRandomEfectResult>();
                    effectParams = effectParams.Skip(1).ToArray();
                    currEffect = effect[0];
                }

                // No random effect defined yet
                if (currEffect == null) continue;

                ranodmEffectResults[currEffect].RandomEfects.Add(effectParams[0],
                                                                 new SingleRandomEfectResult
                                                                 {
                                                                     Variance = double.Parse(effectParams[1]),
                                                                     StdError = double.Parse(effectParams[2]),
                                                                     RandomEffects = new Dictionary<string, double>(),
                                                                 });
            }

            string currRandomEffect = null;
            List<string> currParameterGroup = null;
            bool parametersLine = false;
            foreach (var ranefResultLine in ranefResultLines)
            {
                var lineParts = ranefResultLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (parametersLine)
                {
                    parametersLine = false;
                    currParameterGroup = lineParts;
                    continue;
                }

                if (ranefResultLine.StartsWith("$"))
                {
                    currRandomEffect = ranefResultLine.Substring(1);
                    parametersLine = true;
                    continue;
                }
                
                if (currRandomEffect == null || string.IsNullOrEmpty(ranefResultLine)) continue;

                string valueName = string.Join(" ", lineParts.Take(lineParts.Count - currParameterGroup.Count));
                for (int i = 0; i < currParameterGroup.Count; i++)
                {
                    if (!ranodmEffectResults[currRandomEffect].RandomEfects[currParameterGroup[i]].RandomEffects.ContainsKey(valueName))
                    {
                        ranodmEffectResults[currRandomEffect]
                            .RandomEfects[currParameterGroup[i]]
                            .RandomEffects.Add(valueName,
                                double.Parse(lineParts[i + lineParts.Count - currParameterGroup.Count]));
                    }
                }
                
            }
            return ranodmEffectResults;
        }

        private static ModelComparisons ParseModelComparisonsLme(MixedLinearModel model,
                                                                 IEnumerable<string> modelComparisonAnovas,
                                                                 IEnumerable<string> chiSquareTestLines)
        {
            var modelComparisons = new ModelComparisons
            {
                ComparedModels = new Dictionary<string, ModelComparison>(),
            };

            var comparisonAnovas = modelComparisonAnovas as string[] ?? modelComparisonAnovas.ToArray();
            if (model.RandomEffectVariables.Count() == 1)
            {
                var randomEffect = model.RandomEffectVariables.Single();

                var squareTestLines = chiSquareTestLines as string[] ?? chiSquareTestLines.ToArray();
                var chisqValueLines = BreakRResultToLines(squareTestLines.First(cmd =>
                                                                                cmd.StartsWith("chisq_value\r") || 
                                                                                cmd.StartsWith("chisq_value\n")));
                var pValueLines = BreakRResultToLines(squareTestLines.First(cmd => cmd.StartsWith("pchisq")));

                var chisqValueParams = chisqValueLines[1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var pValueParams = pValueLines[1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                modelComparisons.ComparedModels.Add(randomEffect,
                                                    new ModelComparison
                                                    {
                                                        ChiSq = double.Parse(chisqValueParams[2]),
                                                        ChiDf = double.Parse(chisqValueParams[3].Trim(new [] {'(', ')'}).Split('=')[1]),
                                                        PValue = double.Parse(pValueParams[2]),
                                                    });
            }

            foreach (var modelComparisonAnova in comparisonAnovas)
            {
                var comparisonLines = BreakRResultToLines(modelComparisonAnova);

                var modelsLines = comparisonLines.SkipWhile(line => !line.StartsWith("Models:")).Skip(1)
                    .TakeWhile(line => !line.StartsWith("       Df"));

                var currModel = string.Empty;
                var currModelFormula = string.Empty;
                var models = new Dictionary<string, MixedLinearModel>();
                foreach (var comaprisionLine in modelsLines)
                {
                    var modelName = comaprisionLine.Split(':')[0];
                    if (modelName != currModel)
                    {
                        if (currModelFormula != string.Empty)
                        {
                            models[currModel] = new MixedLinearModel(currModelFormula);
                        }

                        currModel = modelName;
                    }

                    currModelFormula += comaprisionLine.Split(':')[1];
                }
                models[currModel] = new MixedLinearModel(currModelFormula);

                if (models.Count != 2 ||
                    models.Keys.All(mdl => mdl != "model1"))
                {
                    throw new MixedModelException("Inconsistent model ANOVA comparison");
                }

                const string baseModel = "model1";
                var comparedModel = models.Keys.Single(mdl => mdl != "model1");

                var modelStats = comparisonLines.SkipWhile(line => !line.StartsWith("       Df"))
                    .Skip(1)
                    .Take(2)
                    .Select(line => line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                        .Where(r => r != "<")
                        .ToArray())
                    .ToDictionary(line => line[0], line => line);

                modelComparisons.BasedModel = new BaseModelComparison
                {
                    Df = double.Parse(modelStats[baseModel][1]),
                    Aic = double.Parse(modelStats[baseModel][2]),
                    Bic = double.Parse(modelStats[baseModel][3]),
                    LogLik = double.Parse(modelStats[baseModel][4]),
                    Deviance = double.Parse(modelStats[baseModel][5]),
                };

                modelComparisons.ComparedModels.Add(models[comparedModel].ModelFormula,
                    new ModelComparison
                    {
                        Df = double.Parse(modelStats[comparedModel][1]),
                        Aic = double.Parse(modelStats[comparedModel][2]),
                        Bic = double.Parse(modelStats[comparedModel][3]),
                        LogLik = double.Parse(modelStats[comparedModel][4]),
                        Deviance = double.Parse(modelStats[comparedModel][5]),
                        ChiSq = double.Parse(modelStats[baseModel][6]),
                        ChiDf = double.Parse(modelStats[baseModel][7]),
                        PValue = double.Parse(modelStats[baseModel][8]),
                    });
            }

            return modelComparisons;
        }

        private static ResidualStats ParseResidualStatsLme(List<string> modelResultLines,
                                                           List<string> residResultLines)
        {
            var stats = new ResidualStatsLme();

            var quadLines = modelResultLines.SkipWhile(line => !line.StartsWith("Scaled residuals:"))
                                            .Skip(2)
                                            .First()
                                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            stats.Min = double.Parse(quadLines[0]);
            stats.Q1 = double.Parse(quadLines[1]);
            stats.Q2 = double.Parse(quadLines[2]);
            stats.Q3 = double.Parse(quadLines[3]);
            stats.Max = double.Parse(quadLines[4]);
            stats.ResidualsTest = ParseResidualsTest(residResultLines);

            var residLine = modelResultLines.SkipWhile(line => !line.StartsWith("REML criterion at convergence:"))
                                            .First()
                                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            stats.Reml = double.Parse(residLine[4]);

            return stats;
        }


        private static LinearMixedModelResult ParseLinearMixedModelResultLm(MixedLinearModel model,
                                                                ModelDataset dataset,
                                                                List<string> cvglmnetLines,
                                                                List<string> modelResultLines,
                                                                List<string> anovaResultLines,
                                                                List<string> residResultLines,
                                                                IEnumerable<string> leveneTestsRaw,
                                                                string breuschPaganTestsRaw,
                                                                bool sparseResponse,
                                                                List<string> constrastsTestsRaw,
                                                                List<string> influenceRaw,
                                                                string unequalTTestRaw,
                                                                string confidenceRaw)
        {
            var result = new LinearMixedModelResult
            {
                Model = model,
                FixedEffectResults = ParseFixedEffectsLm(model, modelResultLines, dataset),
                RandomEffectResults = sparseResponse ? null : new Dictionary<string, RandomEffectResult>(),
                ModelComparisons = sparseResponse ? null : new ModelComparisons { ComparedModels = new Dictionary<string, ModelComparison>() },
                ModelFitResult = ParseFitResultLm(modelResultLines),
                AnovaResult = ParseAnovaType3(anovaResultLines),
                ResidualStats = sparseResponse ? null : ParseResidualStatsLm(modelResultLines, residResultLines),
                ContrastsTests = ParseContrastsTest(constrastsTestsRaw),
                UnequalVarianceTTest = ParseUnequalVarianceTTest(unequalTTestRaw),
                CvGlmnetVariables = ParseCvGlmnet(cvglmnetLines),
                ModelValidationTest = sparseResponse ? null : 
                    new ModelValidationTests
                    {
                        LeveneTests = ParseLeveneTests(leveneTestsRaw),
                        BreuschPaganTest = ParseBreuschPaganTests(breuschPaganTestsRaw),
                        CookDistanceValues = ParseCookDistances(influenceRaw, dataset, model),
                    },
            };

            if (!sparseResponse)
            {
                //ParseConfidenceInterval(model, confidenceRaw, result);
            }

            return result;
        }

        private static BinomialMixedModelResult ParseLinearMixedModelResultGlm(MixedLinearModel model,
                                                                         ModelDataset dataset,
                                                                         List<string> modelResultLines,
                                                                         List<string> anovaResultLines,
                                                                         IEnumerable<string> leveneTestsRaw,
                                                                         string breuschPaganTestsRaw,
                                                                         List<string> constrastsTestsRaw,
                                                                         List<string> influenceRaw,
                                                                         string unequalTTestRaw,
                                                                         string confidenceRaw)
        {
            var result = new BinomialMixedModelResult
            {
                Model = model,
                FixedEffectResults = ParseFixedEffectsLm(model, modelResultLines, dataset),
                RandomEffectResults = new Dictionary<string, RandomEffectResult>(),
                //ModelComparisons = ParseModelComparisonsLme(model, modelComparisonAnovas, chiSquareTestLines),
                ModelFitResult = new BinomialFitResult(),
            };

            return result;
        }

        private static LinearMixedModelResult ParseLinearMixedModelResultLme(MixedLinearModel model,
                                                                 ModelDataset dataset,
                                                                 List<string> modelResultLines,
                                                                 List<string> anovaResultLines,
                                                                 IEnumerable<string> ranefResultLines,
                                                                 List<string> residResultLines,
                                                                 IEnumerable<string> modelComparisonAnovas,
                                                                 IEnumerable<string> chiSquareTestLines,
                                                                 IEnumerable<string> leveneTestsRaw,
                                                                 string breuschPaganTestsRaw,
                                                                 bool sparseResponse,
                                                                 List<string> constrastsTestsRaw,
                                                                 List<string> influenceRaw,
                                                                 string unequalTTestRaw,
                                                                 string confidenceRaw)
        {
            var result = new LinearMixedModelResult
            {
                Model = model,
                FixedEffectResults = ParseFixedEffectsLme(model, modelResultLines, dataset.DataTable, true),
                RandomEffectResults = sparseResponse ? null : ParseRandomEffectsLme(model, modelResultLines, ranefResultLines),
                ModelComparisons = sparseResponse ? null : ParseModelComparisonsLme(model, modelComparisonAnovas, chiSquareTestLines),
                AnovaResult = ParseAnovaType3(anovaResultLines),
                ResidualStats = sparseResponse ? null : ParseResidualStatsLme(modelResultLines, residResultLines),
                ContrastsTests = ParseContrastsTest(constrastsTestsRaw),
                UnequalVarianceTTest = ParseUnequalVarianceTTest(unequalTTestRaw),
                ModelValidationTest = sparseResponse ? null : 
                    new ModelValidationTests
                    {
                        LeveneTests = ParseLeveneTests(leveneTestsRaw),
                        BreuschPaganTest = ParseBreuschPaganTests(breuschPaganTestsRaw),
                        CookDistanceValues = ParseCookDistances(influenceRaw, dataset, model),
                    },
            };

            if (!sparseResponse)
            {
                //ParseConfidenceInterval(model, confidenceRaw, result);
            }

            return result;
        }

        private static BinomialMixedModelResult ParseLinearMixedModelResultGlme(MixedLinearModel model,
                                                                          ModelDataset dataset,
                                                                          List<string> modelResultLines,
                                                                          List<string> anovaResultLines,
                                                                          IEnumerable<string> ranefResultLines,
                                                                          IEnumerable<string> modelComparisonAnovas,
                                                                          IEnumerable<string> chiSquareTestLines,
                                                                          IEnumerable<string> leveneTestsRaw,
                                                                          string breuschPaganTestsRaw,
                                                                          List<string> constrastsTestsRaw,
                                                                          List<string> influenceRaw,
                                                                          string unequalTTestRaw,
                                                                          string confidenceRaw)
        {
            var result = new BinomialMixedModelResult
            {
                Model = model,
                FixedEffectResults = ParseFixedEffectsLme(model, modelResultLines, dataset.DataTable, false),
                RandomEffectResults = ParseRandomEffectsLme(model, modelResultLines, ranefResultLines),
                ModelComparisons = ParseModelComparisonsLme(model, modelComparisonAnovas, chiSquareTestLines),
                ModelFitResult = ParseFitResultGlm(modelResultLines),
            };

            return result;
        }

        public static MixedModelResult ParseLinearMixedModelResult(MixedLinearModel model,
                                                             ModelDataset dataset,
                                                             IList<string> scriptResponse,
                                                             bool sparseResponse)
        {
            LinearMixedModelResult result;

            var modelSummary = scriptResponse.First(cmd => cmd.Contains("summary(model"));
            var anovaSummary = scriptResponse.FirstOrDefault(cmd => cmd.Contains("Anova(model"));
            List<string> constrastsTestsRaw = scriptResponse.Where(cmd => cmd.Contains("KRmodcomp")).ToList();

            string ranefSummary = null;
            List<string> modelComparisonAnovas = null;
            List<string> chiSquareTestLines = null;
            List<string> cvglmnetLines = null;
            List<string> leveneTestsRaw = null;
            string breuschPaganTestsRaw = null;
            List<string> residResultLines = null;
            List<string> influenceRaw = null;
            string confidenceRaw = null;
            string unequalTTestRaw = null;
            if (!sparseResponse)
            {
                var cvglmnetRaw = scriptResponse.FirstOrDefault(cmd => cmd.Contains("cv.glmnet"));
                cvglmnetLines =  cvglmnetRaw != null ? BreakRResultToLines(cvglmnetRaw) : null;
                ranefSummary = scriptResponse.FirstOrDefault(cmd => cmd.Contains("ranef(model"));
                modelComparisonAnovas = scriptResponse.Where(cmd => cmd.Contains("anova(model")).ToList();
                chiSquareTestLines = scriptResponse.Where(cmd => cmd.Contains("chisq_value")).ToList();
                leveneTestsRaw = scriptResponse.Where(cmd => cmd.StartsWith("modlevene")).ToList();
                breuschPaganTestsRaw = scriptResponse.FirstOrDefault(cmd => cmd.StartsWith("ncvTest("));
                residResultLines = BreakRResultToLines(scriptResponse.FirstOrDefault(cmd => cmd.StartsWith("shapiro.test(residuals(")));
                influenceRaw = scriptResponse.Where(cmd => cmd.Contains("cooks.distance(")).ToList();
                confidenceRaw = scriptResponse.FirstOrDefault(cmd => cmd.Contains("confint("));
                unequalTTestRaw = scriptResponse.FirstOrDefault(cmd => cmd.Contains("t.test("));
            }
            
            var modelResultLines  = BreakRResultToLines(modelSummary);
            var anovaResultLines  = BreakRResultToLines(anovaSummary);

            // Linear model
            if (modelSummary.Contains("lm("))
            {
                result = ParseLinearMixedModelResultLm(model,
                                                 dataset,
                                                 cvglmnetLines,
                                                 modelResultLines,
                                                 anovaResultLines,
                                                 residResultLines,
                                                 leveneTestsRaw,
                                                 breuschPaganTestsRaw,
                                                 sparseResponse,
                                                 constrastsTestsRaw,
                                                 influenceRaw,
                                                 unequalTTestRaw,
                                                 confidenceRaw);
            }

            // Mixed model
            else
            {
                var ranefResultLines = sparseResponse ? null : BreakRResultToLines(ranefSummary);
                result = ParseLinearMixedModelResultLme(model,
                                                  dataset,
                                                  modelResultLines,
                                                  anovaResultLines,
                                                  ranefResultLines,
                                                  residResultLines,
                                                  modelComparisonAnovas,
                                                  chiSquareTestLines,
                                                  leveneTestsRaw,
                                                  breuschPaganTestsRaw,
                                                  sparseResponse,
                                                  constrastsTestsRaw,
                                                  influenceRaw,
                                                  unequalTTestRaw,
                                                  confidenceRaw);
            }

            // Get fitted values and residuals
            if (!sparseResponse) ComputeFittedValuesAndResiduals(model, result, dataset);

            return new MixedModelResult { LinearMixedModelResult = result };
        }

        public static MixedModelResult ParseBinomialMixedModelResult(
            MixedLinearModel model,
            ModelDataset dataset,
            IList<string> scriptResponse)
        {
            BinomialMixedModelResult result;

            var modelSummary = scriptResponse.First(cmd => cmd.Contains("summary(model"));
            var anovaSummary = scriptResponse.FirstOrDefault(cmd => cmd.Contains("Anova(model"));
            List<string> constrastsTestsRaw = scriptResponse.Where(cmd => cmd.Contains("KRmodcomp")).ToList();

            string ranefSummary = null;
            List<string> modelComparisonAnovas = null;
            List<string> chiSquareTestLines = null;
            List<string> leveneTestsRaw = null;
            string breuschPaganTestsRaw = null;
            List<string> influenceRaw = null;
            string confidenceRaw = null;
            string unequalTTestRaw = null;
            ranefSummary = scriptResponse.FirstOrDefault(cmd => cmd.Contains("ranef(model"));
            modelComparisonAnovas = scriptResponse.Where(cmd => cmd.Contains("anova(model")).ToList();
            chiSquareTestLines = scriptResponse.Where(cmd => cmd.Contains("chisq_value")).ToList();
            leveneTestsRaw = scriptResponse.Where(cmd => cmd.StartsWith("modlevene")).ToList();
            breuschPaganTestsRaw = scriptResponse.FirstOrDefault(cmd => cmd.StartsWith("ncvTest("));
            influenceRaw = scriptResponse.Where(cmd => cmd.Contains("cooks.distance(inf)")).ToList();
            confidenceRaw = scriptResponse.FirstOrDefault(cmd => cmd.Contains("confint("));
            unequalTTestRaw = scriptResponse.FirstOrDefault(cmd => cmd.Contains("t.test("));

            var modelResultLines = BreakRResultToLines(modelSummary);
            var anovaResultLines = new List<string>(); // BreakRResultToLines(anovaSummary);

            // Linear model
            if (!modelSummary.Contains("glmer"))
            {
                result = ParseLinearMixedModelResultGlm(model,
                                                  dataset,
                                                  modelResultLines,
                                                  anovaResultLines,
                                                  leveneTestsRaw,
                                                  breuschPaganTestsRaw,
                                                  constrastsTestsRaw,
                                                  influenceRaw,
                                                  unequalTTestRaw,
                                                  confidenceRaw);
            }

            // Mixed model
            else
            {
                var ranefResultLines = BreakRResultToLines(ranefSummary);
                result = ParseLinearMixedModelResultGlme(model,
                                                   dataset,
                                                   modelResultLines,
                                                   anovaResultLines,
                                                   ranefResultLines,
                                                   modelComparisonAnovas,
                                                   chiSquareTestLines,
                                                   leveneTestsRaw,
                                                   breuschPaganTestsRaw,
                                                   constrastsTestsRaw,
                                                   influenceRaw,
                                                   unequalTTestRaw,
                                                   confidenceRaw);
            }

            // Get fitted values and residuals
            // ComputeFittedValuesAndResiduals(model, result, dataset);

            return new MixedModelResult { BinomialMixedModelResult = result };
        }

        public static Dictionary<string, MixedModelResult> ParseLinearMixedModelResults(
            List<MixedLinearModel> models,
            ModelDataset dataset,
            IEnumerable<string> scriptResponses,
            bool sparseResponse)
        {
            var LinearMixedModelResults = new Dictionary<string, MixedModelResult>();
            var responseBatch = new List<string>();
            var modelCount = 0;
            foreach (var scriptResponse in scriptResponses)
            {
                if (scriptResponse.Contains("proc.time"))
                {
                    if (models.Count > modelCount)
                    {
                        var model = models[modelCount++];
                        LinearMixedModelResults[model.ModelFormula] = ParseLinearMixedModelResult(model, 
                                                                                      dataset,
                                                                                      responseBatch,
                                                                                      sparseResponse);
                        responseBatch = new List<string>();                        
                    }
                }
                else
                {
                    responseBatch.Add(scriptResponse);                    
                }
            }

            return LinearMixedModelResults;
        }
    }
}
