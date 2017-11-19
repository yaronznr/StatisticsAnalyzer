using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Helper;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace StatisticsAnalyzerCore.Modeling
{
    public class VariableEffect
    {
        public double MaxEffect { get; set; }
        public double F { get; set; }
        public double Df { get; set; }
        public double PValue { get; set; }
    }

    public class ConfidenceInterval
    {
        public double Low { get; set; }
        public double High { get; set; }
    }

    public class SingleRandomEfectResult
    {
        public double Variance { get; set; }
        public double StdError { get; set; }
        public ConfidenceInterval ConfidenceInterval { get; set; }
        public Dictionary<string, double> RandomEffects { get; set; }        
    }

    public class SingleLevelEfectResult
    {
        public ValueGroupIndex LevelValue { get; set; }
        public double Estimate { get; set; }
        public ConfidenceInterval ConfidenceInterval { get; set; }
        public double StdError { get; set; }
// ReSharper disable InconsistentNaming
        public double TValue { get; set; }
        public double ZValue { get; set; }
        public double PValue { get; set; }
// ReSharper restore InconsistentNaming
    }

    public class FitResult
    {
        public double FValue { get; set; }
        public int UsedDf { get; set; }
        public int DatasetDf { get; set; }
        public double PValue { get; set; }
        public double MultipleR { get; set; }
        public double AdjustedR { get; set; }
    }

    public class BinomialFitResult
    {
        public double Aic { get; set; }
        public double Bic { get; set; }
        public double LogLikelihood { get; set; }
        public double Deviance { get; set; }
        public int UsedDf { get; set; }
        public int DatasetDf { get; set; }
        public double PredictValue { get; set; }
        public double Roc { get; set; }
    }

    public class FixedEffectResult
    {
        public Dictionary<ValueGroupIndex, SingleLevelEfectResult> EffectResults { get; set; }
    }

    public class RandomEffectResult
    {
        public Dictionary<string, SingleRandomEfectResult> RandomEfects { get; set; } 
    }

    public class VarGroupIndex : IEquatable<VarGroupIndex>
    {
        private readonly List<string> _variableNames;

        public VarGroupIndex(string variableName)
        {
            _variableNames = new List<string> { variableName };
        }

        public VarGroupIndex(IEnumerable<string> varNames)
        {
            _variableNames = new List<string>(varNames);
        }

        public override bool Equals(object obj)
        {
            var other = obj as VarGroupIndex;
            return other != null && Equals(other);
        }

        public bool Equals(VarGroupIndex other)
        {
            if (_variableNames.Count != other._variableNames.Count)
            {
                return false;
            }

            for (int i = 0; i < _variableNames.Count; i++)
            {
                if (_variableNames[i] != other._variableNames[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _variableNames.Aggregate(0, (current, str) => current ^ str.GetHashCode());
        }

        public override string ToString()
        {
            return string.Join(",", _variableNames);
        }
    }

    public class ValueGroupIndex : IEquatable<ValueGroupIndex>
    {
        private readonly List<string> _valueNames;

        public List<string> ValueNames { get { return _valueNames;} }

        public ValueGroupIndex() : this(Enumerable.Empty<string>())
        {
        }

        public ValueGroupIndex(string valName)
        {
            _valueNames = new List<string> { valName };
        }

        public ValueGroupIndex(IEnumerable<string> valNames)
        {
            _valueNames = new List<string>(valNames);
        }

        public override bool Equals(object obj)
        {
            var other = obj as VarGroupIndex;
            return other != null && Equals(other);
        }

        public bool Equals(ValueGroupIndex other)
        {
            if (_valueNames.Count != other._valueNames.Count)
            {
                return false;
            }

            for (int i = 0; i < _valueNames.Count; i++)
            {
                if (_valueNames[i] != other._valueNames[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var str in _valueNames)
            {
                hash ^= str.GetHashCode();
            }

            return hash;
        }

        public override string ToString()
        {
            return string.Join(",", _valueNames);
        }
    }

    public class AnovaResult
    {
        public int DegreeFreedom { get; set; }
        public double DegreeFreedomRes { get; set; }
        public double SumSquare { get; set; }
        public double PValue { get; set; }
        public double FValue { get; set; }
    }

    public class ResidualsTest
    {
        public double WValue { get; set; }
        public double PValue { get; set; }
    }

    public class ResidualStats
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Q1 { get; set; }
        public double Q2 { get; set; }
        public double Q3 { get; set; }
        public ConfidenceInterval ConfidenceInterval { get; set; }
        public ResidualsTest ResidualsTest { get; set; }
        public double?[] Residuals { get; set; }
        public double?[] FittedValues { get; set; }
    }

    public class ResidualStatsLm : ResidualStats
    {
        public double Std { get; set; }
        public int Df { get; set; }        
    }

    public class ResidualStatsLme : ResidualStats
    {
        public double Reml { get; set; }
    }

    public class BaseModelComparison
    {
        public double Df { get; set; }
        public double Aic { get; set; }
        public double Bic { get; set; }
        public double LogLik { get; set; }
        public double Deviance { get; set; }
    }

    public class ModelComparison : BaseModelComparison
    {
        public double ChiSq { get; set; }
        public double ChiDf { get; set; }
        public double PValue { get; set; }        
    }

    public class ModelComparisons
    {
        public BaseModelComparison BasedModel { get; set; }
        public Dictionary<string, ModelComparison> ComparedModels { get; set; }
    }

    public class LeveneTest
    {
        public double Df1 { get; set; }
        public double Df2 { get; set; }
        public double FValue { get; set; }
        public double PValue { get; set; }
    }

    public class BreuschPaganTest
    {
        public double Df { get; set; }
        public double ChiSquare { get; set; }
        public double PValue { get; set; }        
    }

    public class CookDistances
    {
        public Dictionary<string, Dictionary<string, double>> RandomCookDistances { get; set; }
        public Dictionary<int, double> ObsCookDistances { get; set; }
    }

    public class ModelValidationTests
    {
        public Dictionary<VarGroupIndex, LeveneTest> LeveneTests { get; set; }
        public BreuschPaganTest BreuschPaganTest { get; set; }
        public CookDistances CookDistanceValues { get; set; }
    }

    public class UnequalVarianceTTest
    {
        public double T { get; set; }
        public double Df { get; set; }
        public double PValue { get; set; }
    }

    public class MixedModelResult
    {
        public LinearMixedModelResult LinearMixedModelResult { get; set; }
        public BinomialMixedModelResult BinomialMixedModelResult { get; set; }
        public IList<string> RawResult
        {
            get
            {
                if (LinearMixedModelResult != null)
                {
                    if (LinearMixedModelResult.RawResult != null)
                    {
                        return LinearMixedModelResult.RawResult;
                    }
                }

                if (BinomialMixedModelResult != null)
                {
                    if (BinomialMixedModelResult.RawResult != null)
                    {
                        return BinomialMixedModelResult.RawResult;
                    }
                }

                return null;
            }
        }
    }

    public class LinearMixedModelResult
    {
        public ResidualStats ResidualStats { get; set; }
        public List<string> CvGlmnetVariables { get; set; }
        public Dictionary<VarGroupIndex, AnovaResult> AnovaResult { get; set; }
        public Dictionary<VarGroupIndex, FixedEffectResult> FixedEffectResults { get; set; }
        public Dictionary<string, RandomEffectResult> RandomEffectResults { get; set; }
        public ModelComparisons ModelComparisons { get; set; }
        public FitResult ModelFitResult { get; set; }
        public ModelValidationTests ModelValidationTest { get; set; }
        public Dictionary<List<int>, AnovaResult> ContrastsTests { get; set; }
        public UnequalVarianceTTest UnequalVarianceTTest { get; set; }
        public IList<string> RawResult { get; set; }
        public MixedLinearModel Model { get; set; }

        private IEnumerable<List<string>> GetSubGroups(List<string> variableGroup)
        {
            if (!variableGroup.Any()) return Enumerable.Empty<List<string>>();

            var removeFirstSubGroups = GetSubGroups(variableGroup.Skip(1).ToList()).ToList();
            return removeFirstSubGroups.Concat(removeFirstSubGroups.Select(sg =>
            {
                var newSg = new List<string>();
                newSg.Add(variableGroup[0]);
                newSg.AddRange(sg);
                return newSg;
            })).Concat(new List<List<string>> { new List<string> {variableGroup[0]}});
        }

        private double GetFittedVariableGroup(IEnumerable<string> variableGroup, DataRow row, TableStats tableStats)
        {
            double value = 0;
            foreach (var subGroup in GetSubGroups(variableGroup.ToList()))
            {
                var subGroupEffect = FixedEffectResults[new VarGroupIndex(subGroup)];
                var subGroupLevels = subGroup.Where(v => row[v] is string).ToList();

                if (subGroupLevels.Count == subGroup.Count)
                {
                    var valueGroupIndex = new ValueGroupIndex(subGroup.Select(v => row[v].ToString()));
                    if (subGroupEffect.EffectResults.ContainsKey(valueGroupIndex))
                    {
                        value += subGroupEffect.EffectResults[valueGroupIndex].Estimate;                        
                    }
                }
                else
                {
                    var valueGroupIndex = subGroupLevels.Count == 0
                        ? (subGroupEffect.EffectResults.ContainsKey(new ValueGroupIndex(string.Empty)) ?
                                new ValueGroupIndex(string.Empty) :
                                new ValueGroupIndex(Enumerable.Empty<string>()))
                        : new ValueGroupIndex(subGroupLevels.Select(v => row[v].ToString()));
                    if (subGroupEffect.EffectResults.ContainsKey(valueGroupIndex))
                    {
                        var slope = subGroupEffect.EffectResults[valueGroupIndex].Estimate;
                        var covariateName = subGroup.Single(v => row[v].GetType() != typeof (string));
                        var regressor = row[covariateName];
                        if (regressor is double)
                        {
                            regressor = regressor.ConvertDouble() - tableStats.ColumnStats[covariateName].ValuesAverage;
                            value += slope * (double)regressor;
                        }
                        else
                        {
                            var val = regressor.ConvertDouble() - tableStats.ColumnStats[covariateName].ValuesAverage;
                            value += slope * val;
                        }                                            
                    }
                }
            }

            return value;
        }

        private double GetFittedRandomFormula(LinearFormula randomFormula, string randomEffect, DataRow row, TableStats tableStats)
        {
            var formulaCoef = RandomEffectResults[randomEffect].
                              RandomEfects.ToDictionary(kvp => kvp.Key,
                                                        kvp => kvp.Value.RandomEffects.ContainsKey(row[randomEffect].ToString().Trim()) ?
                                                            kvp.Value.RandomEffects[row[randomEffect].ToString().Trim()] :
                                                            0.0);

            double value = randomFormula.AllVariables.Contains("0") ? 0.0 : formulaCoef["(Intercept)"];
            foreach (var variable in randomFormula.AllVariables)
            {
                if (variable != "1" && variable != "0")
                {
                    var regressor = row[variable];
                    if (regressor is double)
                    {
                        regressor = regressor.ConvertDouble() - tableStats.ColumnStats[variable].ValuesAverage;
                        value += formulaCoef[variable] * (double)regressor;
                    }
                    else
                    {
                        var val = regressor.ConvertDouble() - tableStats.ColumnStats[variable].ValuesAverage;
                        value += formulaCoef[variable] * val;
                    }
                }
            }

            return value;
        }

        public double? GetFittedValue(DataRow row, TableStats tableStats)
        {
            if (Model.AllVariables.Select(v => row[v]).Any(r => r.IsNull())) return null;

            var linearFormula = Model.GetFixedLinearFormula();

            // model intercept
            double value = FixedEffectResults[new VarGroupIndex("(Intercept)")].EffectResults.First().Value.Estimate;
            foreach (var variableGroup in linearFormula.VariableGroups)
            {
                value += GetFittedVariableGroup(variableGroup, row, tableStats);
            }

            foreach (var randomEffect in Model.RandomEffectVariables)
            {
                var randomFormulas = Model.GetRandomLinearFormulas(randomEffect);

                foreach (var randomFormula in randomFormulas)
                {
                    value += GetFittedRandomFormula(randomFormula, randomEffect, row, tableStats);                    
                }
            }

            return value;
        }

        public List<double?> GetFittedValues(ModelDataset dataSet)
        {
            return (from DataRow row in dataSet.DataTable.Rows select GetFittedValue(row, dataSet.TableStats)).ToList();
        }

    }

    public class BinomialMixedModelResult
    {
        // public ResidualStats ResidualStats { get; set; }

        // public Dictionary<VarGroupIndex, AnovaResult> AnovaResult { get; set; }
        public Dictionary<VarGroupIndex, FixedEffectResult> FixedEffectResults { get; set; }
        public Dictionary<string, RandomEffectResult> RandomEffectResults { get; set; }
        public ModelComparisons ModelComparisons { get; set; }

        public BinomialFitResult ModelFitResult { get; set; }
        // public ModelValidationTests ModelValidationTest { get; set; }
        // public Dictionary<List<int>, AnovaResult> ContrastsTests { get; set; }
        // public UnequalVarianceTTest UnequalVarianceTTest { get; set; }
        
        public IList<string> RawResult { get; set; }
        public MixedLinearModel Model { get; set; }

        private IEnumerable<List<string>> GetSubGroups(List<string> variableGroup)
        {
            if (!variableGroup.Any()) return Enumerable.Empty<List<string>>();

            var removeFirstSubGroups = GetSubGroups(variableGroup.Skip(1).ToList()).ToList();
            return removeFirstSubGroups.Concat(removeFirstSubGroups.Select(sg =>
            {
                var newSg = new List<string>();
                newSg.Add(variableGroup[0]);
                newSg.AddRange(sg);
                return newSg;
            })).Concat(new List<List<string>> { new List<string> { variableGroup[0] } });
        }

        private double GetFittedVariableGroup(IEnumerable<string> variableGroup, DataRow row, TableStats tableStats)
        {
            double value = 0;
            foreach (var subGroup in GetSubGroups(variableGroup.ToList()))
            {
                var subGroupEffect = FixedEffectResults[new VarGroupIndex(subGroup)];
                var subGroupLevels = subGroup.Where(v => row[v] is string).ToList();

                if (subGroupLevels.Count == subGroup.Count)
                {
                    var valueGroupIndex = new ValueGroupIndex(subGroup.Select(v => row[v].ToString()));
                    if (subGroupEffect.EffectResults.ContainsKey(valueGroupIndex))
                    {
                        value += subGroupEffect.EffectResults[valueGroupIndex].Estimate;
                    }
                }
                else
                {
                    var valueGroupIndex = subGroupLevels.Count == 0
                        ? (subGroupEffect.EffectResults.ContainsKey(new ValueGroupIndex(string.Empty)) ?
                                new ValueGroupIndex(string.Empty) :
                                new ValueGroupIndex(Enumerable.Empty<string>()))
                        : new ValueGroupIndex(subGroupLevels.Select(v => row[v].ToString()));
                    if (subGroupEffect.EffectResults.ContainsKey(valueGroupIndex))
                    {
                        var slope = subGroupEffect.EffectResults[valueGroupIndex].Estimate;
                        var covariateName = subGroup.Single(v => row[v].GetType() != typeof(string));
                        var regressor = row[covariateName];
                        if (regressor is double)
                        {
                            regressor = regressor.ConvertDouble() - tableStats.ColumnStats[covariateName].ValuesAverage;
                            value += slope * (double)regressor;
                        }
                        else
                        {
                            var val = regressor.ConvertDouble() - tableStats.ColumnStats[covariateName].ValuesAverage;
                            value += slope * val;
                        }
                    }
                }
            }

            return value;
        }

        private double GetFittedRandomFormula(LinearFormula randomFormula, string randomEffect, DataRow row, TableStats tableStats)
        {
            var formulaCoef = RandomEffectResults[randomEffect].
                              RandomEfects.ToDictionary(kvp => kvp.Key,
                                                        kvp => kvp.Value.RandomEffects.ContainsKey(row[randomEffect].ToString().Trim()) ?
                                                            kvp.Value.RandomEffects[row[randomEffect].ToString().Trim()] :
                                                            0.0);

            double value = randomFormula.AllVariables.Contains("0") ? 0.0 : formulaCoef["(Intercept)"];
            foreach (var variable in randomFormula.AllVariables)
            {
                if (variable != "1" && variable != "0")
                {
                    var regressor = row[variable];
                    if (regressor is double)
                    {
                        regressor = regressor.ConvertDouble() - tableStats.ColumnStats[variable].ValuesAverage;
                        value += formulaCoef[variable] * (double)regressor;
                    }
                    else
                    {
                        var val = regressor.ConvertDouble() - tableStats.ColumnStats[variable].ValuesAverage;
                        value += formulaCoef[variable] * val;
                    }
                }
            }

            return value;
        }

        public double? GetFittedValue(DataRow row, TableStats tableStats)
        {
            if (Model.AllVariables.Select(v => row[v]).Any(r => r.IsNull())) return null;

            var linearFormula = Model.GetFixedLinearFormula();

            // model intercept
            double value = FixedEffectResults[new VarGroupIndex("(Intercept)")].EffectResults.First().Value.Estimate;
            foreach (var variableGroup in linearFormula.VariableGroups)
            {
                value += GetFittedVariableGroup(variableGroup, row, tableStats);
            }

            foreach (var randomEffect in Model.RandomEffectVariables)
            {
                var randomFormulas = Model.GetRandomLinearFormulas(randomEffect);

                foreach (var randomFormula in randomFormulas)
                {
                    value += GetFittedRandomFormula(randomFormula, randomEffect, row, tableStats);
                }
            }

            return Math.Exp(value)/(1+Math.Exp(value));
        }

        public List<double?> GetFittedValues(ModelDataset dataSet)
        {
            return (from DataRow row in dataSet.DataTable.Rows select GetFittedValue(row, dataSet.TableStats)).ToList();
        }

    }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
