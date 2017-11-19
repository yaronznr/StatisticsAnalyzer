using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StatisticsAnalyzerCore.Modeling
{
    public static class MixedModelHelper
    {
        public static IEnumerable<string> GetAllAffectingVariables(MixedLinearModel mixedModel)
        {
            return mixedModel.FixedEffectVariables.Concat(mixedModel.RandomEffectVariables);
        }

        public static IEnumerable<string> GetAllAffectingVariablesExceptOne(MixedLinearModel mixedModel, string excludedVariable)
        {
            return GetAllAffectingVariables(mixedModel).Where(v => v != excludedVariable);
        }
    }

    public class VariableGroup
    {
        private HashSet<string> _variables;
        public IEnumerable<string> Variables { get { return _variables;} }

        public VariableGroup(IEnumerable<string> variables)
        {
            _variables = new HashSet<string>(variables);
        }

        public void AddVariable(string variable)
        {
            if (_variables.Contains(variable))
            {
                //throw new VariableGroupException("Variable already exists in variable group");
            }

            _variables.Add(variable);
        }

        public void RemoveVariable(string variable)
        {
            _variables.Remove(variable);
        }
    }

    public class LinearFormula
    {
        private VariableGroup _allVariables;
        private List<VariableGroup> _variableGroups;

        public IEnumerable<string> AllVariables { get { return _allVariables.Variables;}}
        public List<IEnumerable<string>> VariableGroups { get { return _variableGroups.Select(grp => grp.Variables).ToList();}}

        public LinearFormula()
        {
            _allVariables = new VariableGroup(Enumerable.Empty<string>());
            _variableGroups = new List<VariableGroup>();
        }

        public LinearFormula(string formula)
        {
            Formula = formula;
        }

        public LinearFormula(List<VariableGroup> variableGroups)
        {
            if (variableGroups.Sum(grp => grp.Variables.Count()) > variableGroups.SelectMany(grp => grp.Variables).Distinct().Count())
            {
                throw new LinearModelException("No variable duplication is allowed");
            }

            _variableGroups = variableGroups;
            _allVariables = new VariableGroup(variableGroups.SelectMany(grp => grp.Variables).Distinct());
        }

        public LinearFormula(VariableGroup variables)
        {
            if (variables.Variables.Count() > variables.Variables.Distinct().Count())
            {
                throw new LinearModelException("Each variable can appear only once in formula");
            }

            _allVariables = variables;
            _variableGroups = variables.Variables.Select(var => new VariableGroup(new List<string> {var})).ToList();
        }

        public string Formula
        {
            get
            {
                return string.Join(" + ", _variableGroups.Select(grp => string.Join("*", grp.Variables)));
            }

            set
            {
                var allVariables = new VariableGroup(Enumerable.Empty<string>());
                var variableGroups = new List<VariableGroup>();

                foreach(var varGroupString in value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var variables = varGroupString.Split('*').Select(v => v.Trim()).ToList();
                    variableGroups.Add(new VariableGroup(variables));
                    foreach(var variable in variables)
                    {
                        allVariables.AddVariable(variable);
                    }
                }

                _allVariables = allVariables;
                _variableGroups = variableGroups;
            }
        }

        public void AddVariableGroup(VariableGroup variableGroup)
        {
            foreach (var v in variableGroup.Variables)
            {
                _allVariables.AddVariable(v);
            }

            _variableGroups.Add(variableGroup);
        }

        public void RemoveVariableGroup(int i)
        {
            var removedVariableGroup = _variableGroups[i];

            foreach(var v in removedVariableGroup.Variables)
            {
                _allVariables.RemoveVariable(v);
            }

            _variableGroups.RemoveAt(i);
        }

        public void RemoveVariable(string variable)
        {
            _allVariables.RemoveVariable(variable);
            var grp = _variableGroups.Single(g => g.Variables.Contains(variable));
            if (grp.Variables.Count() > 1)
            {
                grp.RemoveVariable(variable);
            }
            else
            {
                _variableGroups.Remove(grp);
            }
        }

        public void AddVariableToExistingVariableGroup(string variable, int variableGroup)
        {
            _allVariables.AddVariable(variable);
            _variableGroups[variableGroup].AddVariable(variable);
        }
    }

    public class MixedLinearModel
    {
        private Regex c_randomRegex = new Regex(@"(?<=\().+?(?=\))", RegexOptions.Compiled);

        private Dictionary<string, List<LinearFormula>> _linearFormulas;

        public string ModelFormula {
            get
            {
                var sb = new StringBuilder();
                sb.Append(PredictedVariable);
                sb.Append(" ~ ");

                var hasLienarPart = false;
                if (_linearFormulas.ContainsKey(string.Empty))
                {
                    sb.Append(_linearFormulas[string.Empty].Single().Formula);
                    hasLienarPart = true;
                }
                if (RandomEffectVariables.Any())
                {
                    if (hasLienarPart)
                    {
                        sb.Append(" + ");
                    }
                    sb.Append(string.Join(" + ", 
                                          _linearFormulas.Where(kvp => !string.IsNullOrEmpty(kvp.Key))
                                                         .Select(kvp => string.Join(" + ",
                                                                                    kvp.Value.Select(f => string.Format("({0}|{1})",
                                                                                                                        f.Formula,
                                                                                                                        kvp.Key))))));
                }
                else
                {
                    if (!hasLienarPart)
                    {
                        sb.Append("1");
                    }
                }
                return sb.ToString();
            }

            set
            {
                var splitFormula = value.Split('~');
                var predictedVariable = splitFormula[0].Trim();
                var linearFormulas = new Dictionary<string, List<LinearFormula>>();

                var formulaPart = splitFormula[1].Trim();
                foreach (Match match in c_randomRegex.Matches(formulaPart))
                {
                    var randomEffectSection = match.Groups[0].Value.Split('|');
                    var linearFormula = new LinearFormula(randomEffectSection[0]);

                    var randomEffect = randomEffectSection[1].Trim();
                    if (!linearFormulas.ContainsKey(randomEffect))
                    {
                        linearFormulas.Add(randomEffect, new List<LinearFormula>());
                    }

                    linearFormulas[randomEffect].Add(linearFormula);
                }

                var fixedFormulaString = string.Join("+", c_randomRegex.Replace(formulaPart, string.Empty).
                                                                        Split('+').
                                                                        Where(term => !term.Contains("(")));
                var fixedFormula = new LinearFormula(fixedFormulaString);
                if (!string.IsNullOrEmpty(fixedFormulaString))
                {
                    linearFormulas.Add(string.Empty, new List<LinearFormula>{fixedFormula});
                }

                _linearFormulas = linearFormulas;
                PredictedVariable = predictedVariable;
            }
        }

        public bool IsLogistic { get { return false; } }
        public string PredictedVariable { get; set; }
        public IEnumerable<string> RandomEffectVariables { get { return _linearFormulas.Keys.Where(v => !string.IsNullOrEmpty(v)); } }
        public IEnumerable<string> FixedEffectVariables
        {
            get
            {
                return _linearFormulas.ContainsKey(string.Empty)
                    ? _linearFormulas[string.Empty].Single().AllVariables.Where(v => v != "1")
                    : Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> AllVariables
        {
            get { return FixedEffectVariables.Concat(
                         RandomEffectVariables).Concat(
                         RandomEffectVariables.SelectMany(GetRandomLinearFormulas)
                                              .SelectMany(x => x.AllVariables)
                                              .Except(new [] {"0", "1"})).Concat(
                         new[] { PredictedVariable });
            }
        }

        public bool HasIterator
        {
            get { return ModelFormula.Contains("{"); }
        }

        public MixedLinearModel(IEnumerable<string> fixedEffectVariables,
                                IEnumerable<string> randomEffectVariables,
                                string predictedVariable)
        {
            PredictedVariable = predictedVariable;

            _linearFormulas = 
                randomEffectVariables.ToDictionary(v => v,
                                                   v => new List<LinearFormula>
                                                   {
                                                       new LinearFormula(new VariableGroup(new List<string> { "1" }))
                                                   });
            _linearFormulas.Add(string.Empty, new List<LinearFormula> { new LinearFormula(new VariableGroup(fixedEffectVariables)) });
        }

        public MixedLinearModel(string formula)
        {
            ModelFormula = formula;
        }

        public MixedLinearModel Clone()
        {
            return new MixedLinearModel(ModelFormula);
        }

        public LinearFormula GetFixedLinearFormula()
        {
            var splitFormula = ModelFormula.Split('~');
            var formulaPart = splitFormula[1].Trim();
            var fixedFormulaString = string.Join("+", c_randomRegex.Replace(formulaPart, string.Empty).
                                                        Split('+').
                                                        Where(term => !term.Contains("(")));
            return new LinearFormula(fixedFormulaString);
        }

        public LinearFormula GetRandomLinearFormula(string randomVariable)
        {
            var splitFormula = ModelFormula.Split('~');
            var formulaPart = splitFormula[1].Trim();
            foreach (Match match in c_randomRegex.Matches(formulaPart))
            {
                var randomEffectSection = match.Groups[0].Value.Split('|');
                var linearFormula = new LinearFormula(randomEffectSection[0]);

                if (randomEffectSection[1] == randomVariable)
                {
                    return linearFormula;
                }
            }

            throw new LinearModelException("No such random effect variable");
        }

        public IEnumerable<LinearFormula> GetRandomLinearFormulas(string randomVariable)
        {
            var splitFormula = ModelFormula.Split('~');
            var formulaPart = splitFormula[1].Trim();
            foreach (Match match in c_randomRegex.Matches(formulaPart))
            {
                var randomEffectSection = match.Groups[0].Value.Split('|');
                var linearFormula = new LinearFormula(randomEffectSection[0]);

                if (randomEffectSection[1] == randomVariable)
                {
                    yield return linearFormula;
                }
            }
        }

        public void RemoveRandomEffectPart(string randomEffect)
        {
            _linearFormulas.Remove(randomEffect);
        }

        public void RemoveRandomEffectFormulaVariable(string randomEffect, string slopeVariable)
        {
            _linearFormulas[randomEffect] =
                _linearFormulas[randomEffect].Where(lf => !lf.AllVariables.Contains(slopeVariable) || 
                                                          !lf.AllVariables.Contains("0") ||
                                                           lf.AllVariables.Count() != 2)
                                             .ToList();
            foreach (var linearFormula in _linearFormulas[randomEffect])
            {
                if (linearFormula.AllVariables.Contains(slopeVariable))
                {
                    linearFormula.RemoveVariable(slopeVariable);

                    if (slopeVariable == "1")
                    {
                        linearFormula.AddVariableToExistingVariableGroup("0", 0);
                    }
                }
            }
        }

        public void DecorrelateAllCovariates()
        {
            var randomVars = _linearFormulas.Keys.Distinct().ToList();

            foreach (var randomVar in randomVars.Except(new List<string>{""}))
            {
                var toDecorrelate = _linearFormulas[randomVar].Where(f => f.AllVariables.Contains("1") && f.AllVariables.Count() > 1).ToList();
                
                _linearFormulas[randomVar] = _linearFormulas[randomVar].Where(f => !f.AllVariables.Contains("1") || f.AllVariables.Count() == 1).ToList();
                foreach (var linearFormula in toDecorrelate)
                {
                    var vars = new List<string> {"0"};
                    vars.AddRange(linearFormula.AllVariables.Except(new List<string> { "1" }));
                    _linearFormulas[randomVar].Add(new LinearFormula(vars.Select(v => new VariableGroup(new List<string> {v})).ToList()));
                }
                _linearFormulas[randomVar].Add(new LinearFormula("1"));
            }
        }

        public void RemoveFixedEffect(string fixedEffect)
        {
            _linearFormulas[string.Empty].Single().RemoveVariable(fixedEffect);
        }

        public void AndFixedEffect(string fixedEffect)
        {
            if (!_linearFormulas.ContainsKey(string.Empty))
            {
                _linearFormulas[string.Empty] = new List<LinearFormula> { new LinearFormula(fixedEffect) };
            }
            else
            {
                _linearFormulas[string.Empty].Single().AddVariableGroup(new VariableGroup(new[] { fixedEffect }));                
            }
        }

        public void AndRandomEffect(string randomEffect)
        {
            _linearFormulas[randomEffect] = new List<LinearFormula> { new LinearFormula("1") };
        }

        public void AndRandomCovariate(string randomEffect, string covariate)
        {
            _linearFormulas[randomEffect].First().AddVariableGroup(new VariableGroup(new List<string> {covariate}));
        }

        public void RemoveIterator()
        {
            var fixedIterator = FixedEffectVariables.First(e => e.Contains("{"));
            RemoveFixedEffect(fixedIterator);
        }

        public string GetIteratorName()
        {
            if (!HasIterator) return string.Empty;
            var iterator = FixedEffectVariables.First(e => e.Contains("{"));
            return iterator.Substring(1, iterator.Length - 2);
        }
    }
}
