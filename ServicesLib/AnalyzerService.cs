using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ExcelLib;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;
using StatisticsAnalyzerCore.Questions;
using System.Collections.Generic;
using System.Linq;
using StatisticsAnalyzerCore.R;

namespace ServicesLib
{
    public class AnalyzerService
    {
        private readonly QuestionAnalyzer _questionAnalyzer;

        private IList<string> RunModelScript(
            ModelDataset dataset, 
            List<MixedLinearModel> mixedModels,
            bool sparseComputation,
            List<int> contrasts)
        {
            var fullVariableList = mixedModels.SelectMany(m => m.AllVariables).Distinct().ToList();
            var variableNameReplacement = new Dictionary<string, string>();
            int varCount = 100000;
            foreach (var variable in fullVariableList)
            {
                variableNameReplacement[variable] = string.Format("var{0}", varCount++);
            }

            var script = RScriptFactory.CreateMixedModelScript(mixedModels, dataset, sparseComputation, contrasts);
            foreach (var variable in fullVariableList.OrderByDescending(s => s.Length))
            {
                script = script.Replace(RScriptFactory.WrapVariable(variable), variableNameReplacement[variable]);
            }

            var memoryStream = new MemoryStream();
            DataDocument.EncodeDataTable(memoryStream, 
                                         dataset,
                                         fullVariableList.ToList(),
                                         new HashSet<string>(
                                             mixedModels.First()
                                                        .RandomEffectVariables
                                                        .Union(new List<string> {mixedModels.First().PredictedVariable})),
                                         variableNameReplacement);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[memoryStream.Length];
            memoryStream.Read(bytes, 0, (int)memoryStream.Length);
            var response =  ServiceContainer.RService().RunRScript(script, bytes);

            foreach (var variable in fullVariableList.OrderByDescending(s => variableNameReplacement[s]))
            {
                response = response.Replace(variableNameReplacement[variable], variable);
            }

            var outputs = response.Split(new[] { "> " }, StringSplitOptions.RemoveEmptyEntries);
            return outputs;
        }

        private string FormatRCommand(string rCommand)
        {
            var rLines = rCommand.Split(new[] {"\r\n"}, StringSplitOptions.None);

            var sb = new StringBuilder();
            sb.AppendFormat("<div style='color:blue;font-weight:bold;'>&gt; {0}</div><br>", rLines[0].Replace(" ", "&nbsp;"));
            sb.Append(rLines.Skip(1).All(string.IsNullOrEmpty)
                ? "NO_RESPONSE"
                : string.Join("<br>", rLines.Skip(1).Select(e => e.Replace(" ", "&nbsp;"))));
            return sb.ToString();
        }

        private Answer GetLogRAnswer(IEnumerable<string> scriptResponse)
        {
            var question = new SimpleQuestion("Script Log (raw):",
                                              string.Join("<br><br>", scriptResponse.Select(FormatRCommand))
                                                    .Replace("{",string.Empty)
                                                    .Replace("}",string.Empty)
                                                    .Replace("\r\n", "<br>")
                                                    .Replace("\n", "<br>")
                                                    .Replace("\r", "<br>"),
                                              QuestionId.ModelAnova);
            return question.AnalyzeAnswer(null, null, null);
        }

        private void HandleConvergenceIssues(List<Answer> answers, IEnumerable<string> scriptResponse)
        {
            var responses = scriptResponse as string[] ?? scriptResponse.ToArray();
            if (responses.Any(s => s.Contains("Downdated VtV is not positive definite")) ||
                responses.Any(s => s.Contains("Model failed to converge: degenerate  Hessian with 1 negative eigenvalues")) ||
                responses.Any(s => s.Contains("Some predictor variables are on very different scales: consider rescaling")))
            {
                var question = new SimpleQuestion("Model Convergence:",
                                                  "Model failed to converge. This can happen when model is overly complicated or data " +
                                                  "does not fit the formula.",
                                                  QuestionId.ConvergenceIssues);
                answers.Add(question.AnalyzeAnswer(null, null, null));
            }

            if (responses.Any(s => s.Contains("there are aliased coefficients in the model")))
            {
                var question = new SimpleQuestion("Model Aliasing:",
                                                  "Model has aliased responsed. This means that at least two variables are identical " +
                                                  "(i.e linealy dependant). To fix issue please make model less redundant by removing " +
                                                  "the offeding variable.",
                                                  QuestionId.ConvergenceIssues);
                answers.Add(question.AnalyzeAnswer(null, null, null));
            }
        }

        public AnalyzerService()
        {
            _questionAnalyzer = new QuestionAnalyzer();

            _questionAnalyzer.RegisterQuestionFactory(new DataExploreQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new RegressionSummaryQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new ModelValidityQuestion.ModelValidityQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new SingleVariableTwoValuesQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new SingleVariableMultipleValuesQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new IsolatedLinearVariableQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new NWayAnovaQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new SimpleRandomEffectBiasQuestionFactory());
            _questionAnalyzer.RegisterQuestionFactory(new RandomCovariateQuestionFactory());
        }

        public IEnumerable<Question> GetQuestions(MixedLinearModel mixedModel, ModelDataset dataset)
        {
            if (mixedModel.HasIterator)
            {
                var nullModel = mixedModel.Clone();
                nullModel.RemoveIterator();
                return new List<Question>
                       {
                           new MultiTestAnalysisQuestion
                           {
                               QuestionId = QuestionId.ModelSummary,
                               QuestionInterpertTemplate = "Which variable contributes most significantly against null model '{0}'",
                               QuestionParameters = new List<string>
                               {
                                   nullModel.ModelFormula,
                               }
                           }
                       };
            }

            try
            {
                return _questionAnalyzer.AnalyzeDatasetSupportedQuestions(dataset, mixedModel);

            }
            catch (Exception) // Return empty question list when analysis fails
            {
                return Enumerable.Empty<Question>();
            }
        }

        public List<Answer> GetAnswers(string userName, MixedLinearModel mixedModel)
        {
            // Fetch data table
            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument(userName);
            return GetAnswers(userName, mixedModel, dataset, RunModelScript(dataset, 
                                                                            new List<MixedLinearModel> { mixedModel },
                                                                            false,
                                                                            new List<int>()));
        }

        public List<Answer> GetAnswers(string userName,
                                       MixedLinearModel mixedModel,
                                       ModelDataset dataset,
                                       IList<string> scriptResponse)
        {
            var answers = new List<Answer>();

            try
            {
                var questions = GetQuestions(mixedModel, ServiceContainer.ExcelDocumentService().GetExcelDocument(userName));

                if (dataset.DataTable.Columns[mixedModel.PredictedVariable].DataType != typeof(string))
                {
                    // Parse script
                    var result = RMixedModelResultParser.ParseLinearMixedModelResult(mixedModel,
                                                                                     dataset,
                                                                                     scriptResponse,
                                                                                     false);

                    // Save result or further inquiries
                    result.LinearMixedModelResult.RawResult = scriptResponse;
                    dataset.ModelResult = result;

                    answers.AddRange(questions.Select(q => q.AnalyzeAnswer(dataset, mixedModel, result)));
                }
                else
                {
                    // Parse script
                    var result = RMixedModelResultParser.ParseBinomialMixedModelResult(mixedModel,
                                                                                       dataset,
                                                                                       scriptResponse);

                    // Save result or further inquiries
                    result.BinomialMixedModelResult.RawResult = scriptResponse;
                    dataset.ModelResult = result;

                    answers.AddRange(questions.Select(q => q.AnalyzeAnswer(dataset, mixedModel, result)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            HandleConvergenceIssues(answers, scriptResponse);
            answers.Add(GetLogRAnswer(scriptResponse));
            return answers;
        }

        public Dictionary<string, VariableEffect> PerformMultipleAnalysis(string rawResult)
        {
            var d = new Dictionary<string, VariableEffect>();
            using (var reader = new StringReader(rawResult))
            {
                reader.ReadLine(); // Skip title line

                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    var parts = line.Split('\t');
                    d[parts[0]] = new VariableEffect
                    {
                        MaxEffect = double.Parse(parts[1]),
                        F = double.Parse(parts[2]),
                        Df = double.Parse(parts[3]),
                        PValue = double.Parse(parts[4]),
                    };
                }
            }

            return d;
        }

        public Dictionary<string, VariableEffect> PerformMultipleAnalysis(string userName, string formulaTemplate)
        {
            var d = new Dictionary<string, VariableEffect>();
            var lockObject = new object();

            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument(userName);
            var sb = new StringBuilder();
            sb.AppendLine("Variable\tEffect\tF\tDF\tPVal");

            var iteratorModel = new MixedLinearModel(formulaTemplate);
            var regexString = string.Format("^{0}$",
                                            iteratorModel.GetIteratorName()
                                                            .Replace("?", "\\w*")
                                                            .Replace("group", "(?<group>[\\w.]*)"));
            var regex = new Regex(regexString, RegexOptions.Compiled);

            var vars = dataset.TableStats
                                .ColumnStats
                                .Keys
                                .Where(v => v != "uniqueid")
                                .Where(v => !formulaTemplate.Contains(v) && 
                                            dataset.TableStats.ColumnStats[v].ValuesCount.Count > 1 &&
                                            regex.IsMatch(v));

            var nullModel = iteratorModel.Clone();
            nullModel.RemoveIterator();

            var groups = vars.GroupBy(a => regex.Match(a).Groups["group"].Value).ToList();
            var groupsMap = groups.ToDictionary(g => g.Key, g => g);
            var action = new Action<List<IGrouping<string, string>>>(groups1 =>
            {
                var variableMap = new Dictionary<string, string>();
                var mixedModels = new List<MixedLinearModel>();
                foreach (var variableGroup in groups1)
                {
                    var model = new MixedLinearModel(formulaTemplate);
                    model.RemoveIterator();
                    foreach (var variable in variableGroup)
                    {
                        model.AndFixedEffect(variable);
                    }

                    mixedModels.Add(model);
                    variableMap[model.ModelFormula] = variableGroup.Key;
                }

                try
                {
                    // Run and parse script
                    var response = RunModelScript(dataset,
                                                  mixedModels,
                                                  true,
                                                  new List<int> { nullModel.FixedEffectVariables.Count() });
                    var results = RMixedModelResultParser.ParseLinearMixedModelResults(mixedModels, dataset, response, true);

                    lock (lockObject)
                    {
                        foreach (var mixedModel in mixedModels)
                        {
                            var variable = variableMap[mixedModel.ModelFormula];
                            if (groupsMap[variable].Count() == 1)
                            {
                                var varAnova = results[mixedModel.ModelFormula].LinearMixedModelResult
                                                                               .AnovaResult[new VarGroupIndex(groupsMap[variable].Single())];
                                var varEffectResults = results[mixedModel.ModelFormula].LinearMixedModelResult
                                                                                       .FixedEffectResults[new VarGroupIndex(groupsMap[variable].Single())].EffectResults;
                                var varMaxEffect = Math.Max(0, varEffectResults.Values.Max(e => e.Estimate)) -
                                                    Math.Min(0, varEffectResults.Values.Min(e => e.Estimate));

                                d[variable] = new VariableEffect
                                {
                                    MaxEffect = varMaxEffect,
                                    Df = varAnova.DegreeFreedomRes,
                                    F = varAnova.FValue,
                                    PValue = varAnova.PValue,
                                };
                                sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                                                            variable,
                                                            varMaxEffect,
                                                            varAnova.FValue,
                                                            varAnova.DegreeFreedomRes,
                                                            varAnova.PValue));
                            }
                            else
                            {
                                var varAnova = results[mixedModel.ModelFormula].LinearMixedModelResult
                                                                               .ContrastsTests
                                                                               .First()
                                                                               .Value;
                                d[variable] = new VariableEffect
                                {
                                    MaxEffect = -1,
                                    Df = varAnova.DegreeFreedomRes,
                                    F = varAnova.FValue,
                                    PValue = varAnova.PValue,
                                };
                                sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
                                                            variable,
                                                            -1,
                                                            varAnova.FValue,
                                                            varAnova.DegreeFreedomRes,
                                                            varAnova.PValue));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            foreach (var variableGroups in groups.Partition(20))
            {
                //actions.Enqueue(variableGroups);
                action(variableGroups);
            }

            /*var threads = new List<Thread>();
            const int threadCount = 20;
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(() =>
                {
                    List<IGrouping<string, string>> varGroup = null;
                    lock (actions)
                    {
                        if (actions.Count > 0)
                        {
                            varGroup = actions.Dequeue();
                        }
                    }
                    while (varGroup != null)
                    {
                        action(varGroup);
                        lock (actions)
                        {
                            varGroup = actions.Count > 0 ? actions.Dequeue() : null;
                        }
                    }
                });
                thread.Start();
                threads.Add(thread);
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }*/

            ServiceContainer.StorageService()
                            .SetModelAnalysis(userName, 
                                              ServiceContainer.StorageService().GetCurrentExcelName(userName), 
                                              sb.ToString());

            return d;
        }
    }
}
