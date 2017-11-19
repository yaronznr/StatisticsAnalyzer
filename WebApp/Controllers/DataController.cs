using System.Globalization;
using ServicesLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;
using StatisticsAnalyzerCore.Helper;
using StatisticsAnalyzerCore.Modeling;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class DataController : ApiController
    {
        private double ToDouble(object dataPoint)
        {
            if (dataPoint is int)
            {
                return ((int)dataPoint);
            }

            return double.Parse(dataPoint.ToString());
        }

        private IEnumerable<int> GetRange(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return i;
            }
        }

        private DataModel DoFlatModel(DataTable dataTable,
                                      List<string> variables,
                                      string predictingVariable,
                                      AggregationType aggregationType)
        {
            var groups = new Dictionary<ValueGroupIndex, List<object>>();
            foreach (DataRow row in dataTable.Rows)
            {
                if (variables.Any(var => row[var] == DBNull.Value) || variables.Any(var => row[var] == null) ||
                    row[predictingVariable] == DBNull.Value || row[predictingVariable] == null)
                {
                    continue;
                }

                var row1 = row;
                var groupingIndex = new ValueGroupIndex(variables.Select(var => row1[var].ToString()));

                if (!groups.ContainsKey(groupingIndex))
                {
                    groups.Add(groupingIndex, new List<object>());
                }

                groups[groupingIndex].Add(row[predictingVariable]);
            }

            IEnumerable<KeyValuePair<ValueGroupIndex, List<object>>> orderedGroups = groups;
            for (int i = variables.Count - 1; i >= 0; i--)
            {
                if (dataTable.Columns[variables[i]].DataType == typeof(string))
                {
                    orderedGroups = orderedGroups.OrderBy(grp => grp.Key.ValueNames[i]).ToList();
                }
                else
                {
                    orderedGroups = orderedGroups.OrderBy(grp =>
                    {
                        try
                        {
                            return double.Parse(grp.Key.ValueNames[i]);
                        }
                        catch (Exception)
                        {
                            return 0.0;
                        }
                    }).
                    ToList();
                }
            }

            int index = dataTable.Rows[0].Table.Columns.IndexOf(predictingVariable);
            var zeroLevelValue = dataTable.Rows[0][index] as string;

            if (aggregationType == AggregationType.None)
            {
                var valueList = orderedGroups.Where(grp => grp.Value.Count > 0).Select(
                    grp =>
                    new GroupingIndexValue
                    {
                        GroupingVariable =
                            GetRange(grp.Key.ValueNames.Count).Select(idx => new Tuple<string, string>(variables[idx], grp.Key.ValueNames[idx]))
                                                              .ToList(),
                        Value = string.Equals(grp.Value.First(), zeroLevelValue as string) ? "0" : "1",
                    }).ToList();

                return new DataModel {ValueList = valueList};
            }

            if (aggregationType == AggregationType.Lowess)
            {
                var valueList = orderedGroups.Where(grp => grp.Value.Count > 0)
                                             .ToDictionary(grp => double.Parse(grp.Key.ValueNames[0]),
                                                           grp => grp.Value.Sum(v => string.Equals(v, zeroLevelValue as string) ? 0.0 : 1.0) / grp.Value.Count());
                var countList = orderedGroups.Where(grp => grp.Value.Count > 0)
                                             .ToDictionary(grp => double.Parse(grp.Key.ValueNames[0]),
                                                           grp => 1);

                var min = valueList.Keys.Min();
                var max = valueList.Keys.Max();
                var coef = (countList.Sum(kvp => kvp.Value) / 10.0) / (min - max);
                Func<object, object, double> distanceWeight = (var1, var2) =>
                {
                    return Math.Exp(coef * Math.Abs(var1.ConvertDouble() - var2.ConvertDouble()));
                };

                return new DataModel
                {
                    ValueList = valueList.Select(
                        kvp =>
                        new GroupingIndexValue
                        {
                            GroupingVariable = new List<Tuple<string, string>>
                            {
                                new Tuple<string, string>("prob", kvp.Key.ConvertDouble().ToString())
                            },
                            Value = (valueList.Sum(kvp2 => kvp2.Value * countList[kvp2.Key] * distanceWeight(kvp2.Key, kvp.Key)) /
                                    valueList.Sum(kvp2 => countList[kvp2.Key] * distanceWeight(kvp2.Key, kvp.Key))).ToString(),
                        }).ToList(),
                };
            }

            return null;
        }

        private DataModel DoAggregate(DataTable dataTable, 
                                      List<string> variables,
                                      string predictingVariable,
                                      Func<List<object>, string> aggregator)
        {
            if (variables.Any(var => dataTable.Columns[var].DataType != typeof(string)))
            {
                //throw new Exception("Trying to group non-string columns");
            }

            var groups = new Dictionary<ValueGroupIndex, List<object>>();
            foreach (DataRow row in dataTable.Rows)
            {
                if (variables.Any(var => row[var] == DBNull.Value) || variables.Any(var => row[var] == null) ||
                    row[predictingVariable] == DBNull.Value || row[predictingVariable] == null)
                {
                    continue;
                }

                var row1 = row;
                var groupingIndex = new ValueGroupIndex(variables.Select(var => row1[var].ToString()));

                if (!groups.ContainsKey(groupingIndex))
                {
                    groups.Add(groupingIndex, new List<object>());
                }

                groups[groupingIndex].Add(row[predictingVariable]);
            }

            IEnumerable<KeyValuePair<ValueGroupIndex, List<object>>> orderedGroups = groups;

            for (int i = variables.Count-1; i >= 0; i--)
            {
                if (dataTable.Columns[variables[i]].DataType == typeof(string))
                {
                    orderedGroups = orderedGroups.OrderBy(grp => grp.Key.ValueNames[i]).ToList();
                }
                else
                {
                    orderedGroups = orderedGroups.OrderBy(grp =>
                    {
                        try
                        {
                            return double.Parse(grp.Key.ValueNames[i]);
                        }
                        catch (Exception)
                        {
                            return 0.0;
                        }
                    }).
                    ToList();
                }
            }

            List<GroupingIndexValue> valueList;
            var firstGrouperCount = orderedGroups.Select(e => e.Key.ValueNames[0]).Distinct().Count();
            if (firstGrouperCount > 10 && dataTable.Columns[variables[0]].DataType == typeof(string))
            {
                valueList = orderedGroups.Where(grp => grp.Value.Count > 0).Select(
                    grp =>
                    new GroupingIndexValue
                    {
                        GroupingVariable = GetRange(grp.Key.ValueNames.Count)
                                        .Select(idx => new Tuple<string, string>(
                                            variables[idx], idx > 1 ? 
                                                grp.Key.ValueNames[idx] :
                                                string.Join("", grp.Key.ValueNames[idx].Take(10).SelectMany(e => new[] { e.ToString(CultureInfo.InvariantCulture), "<br>" }))))
                                        .ToList(),
                        Value = aggregator(grp.Value),
                    }).ToList();                
            }
            else
            {
                valueList = orderedGroups.Where(grp => grp.Value.Count > 0).Select(
                    grp =>
                    new GroupingIndexValue
                    {
                        GroupingVariable = GetRange(grp.Key.ValueNames.Count)
                                        .Select(idx => new Tuple<string, string>(variables[idx], grp.Key.ValueNames[idx]))
                                        .ToList(),
                        Value = aggregator(grp.Value),
                    }).ToList();
            }

            return new DataModel {ValueList = valueList};
        }

        private enum AggregationType
        {
            None,
            Lowess,
            Count,
            Sum,
            Average,
            Stddev,
        }

        private DataModel DoAggregation(AggregationType aggregationType)
        {
            var formulaTask = Request.Content.ReadAsStringAsync();
            formulaTask.Wait();
            var formula = formulaTask.Result;

            var groupingVariables = formula.Split(':')[0];
            var predictingVariable = formula.Split(':')[1];

            var userName = User.Identity.Name;
            var dataTable = ServiceContainer.ExcelDocumentService().GetExcelDocument(userName).DataTable;

            if (dataTable.Columns[predictingVariable].DataType == typeof(string) &&
                aggregationType != AggregationType.Count &&
                aggregationType != AggregationType.None &&
                aggregationType != AggregationType.Lowess)
            {
                double db;
                if (dataTable.Rows.Cast<DataRow>().Any(row => !double.TryParse(row[predictingVariable].ToString(), out db)))
                {
                    throw new Exception("Trying to do arithmetic on categorical variable");
                }
            }

            Func<List<object>, string> aggregator = null;
            switch (aggregationType)
            {
                case AggregationType.Average:
                    aggregator = list => list.Average(val => ToDouble(val)).ToString(CultureInfo.InvariantCulture);
                    break;

                case AggregationType.Count:
                    aggregator = list => list.Count().ToString(CultureInfo.InvariantCulture);
                    break;

                case AggregationType.Stddev:
                    aggregator = list =>
                    {
                        var average = list.Average(val => ToDouble(val));
                        var averageSqr = list.Average(val => ToDouble(val) * ToDouble(val));
                        return Math.Sqrt(averageSqr - (average * average)).ToString(CultureInfo.InvariantCulture);
                    };
                    break;

                case AggregationType.Sum:
                    aggregator = list => list.Sum(val => ToDouble(val)).ToString(CultureInfo.InvariantCulture);
                    break;
            }

            if (aggregationType == AggregationType.None || aggregationType == AggregationType.Lowess)
            {
                return DoFlatModel(dataTable,
                                   groupingVariables.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                                   predictingVariable,
                                   aggregationType);
            }

            return DoAggregate(dataTable, 
                               groupingVariables.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(), 
                               predictingVariable, 
                               aggregator);
        }

        [HttpPost]
        public DataModel Count()
        {
            return DoAggregation(AggregationType.Count);
        }

        [HttpPost]
        public DataModel Sum()
        {
            return DoAggregation(AggregationType.Sum);
        }

        [HttpPost]
        public DataModel Average()
        {
            return DoAggregation(AggregationType.Average);
        }

        [HttpPost]
        public DataModel Stddev()
        {
            return DoAggregation(AggregationType.Stddev);
        }

        [HttpPost]
        public DataModel Value()
        {
            return DoAggregation(AggregationType.None);
        }

        [HttpPost]
        public DataModel Lowess()
        {
            return DoAggregation(AggregationType.Lowess);
        }

        [HttpPost]
        public DataModel Residuals()
        {
            var formulaTask = Request.Content.ReadAsStringAsync();
            formulaTask.Wait();
            var formula = formulaTask.Result;

            var modelResults = ServiceContainer.ExcelDocumentService().GetExcelDocument(User.Identity.Name).ModelResult;

            // Check at least one row has no null
            if (modelResults.LinearMixedModelResult.ResidualStats.Residuals.All(x => x.IsNull()))
            {
                throw new Exception("No applicable row");
            }

// ReSharper disable PossibleInvalidOperationException
            var residMean = modelResults.LinearMixedModelResult.ResidualStats.Residuals.Average().Value;
            var residStd = Math.Sqrt(modelResults.LinearMixedModelResult.ResidualStats.Residuals.Select(e => (e - residMean) * (e - residMean)).Average().Value);
// ReSharper restore PossibleInvalidOperationException

            if (formula == "QQ")
            {
                double residCount = modelResults.LinearMixedModelResult.ResidualStats.Residuals.Where(r => r.HasValue).Count() + 1;
                double i = 0.5;
                return new DataModel
                {
                    ValueList = modelResults.LinearMixedModelResult
                                            .ResidualStats
                                            .Residuals
                                            .OrderBy(e => e)
                                            .Where(e => e.HasValue)
                                            .Select(resid => new GroupingIndexValue
                                            {
                                                GroupingVariable = new List<Tuple<string, string>>
                                                {
                                                    new Tuple<string, string>("RowId",
                                                                              DistributionHelper.ComputeInverseZ(i++/residCount)
                                                                                                .ToString(CultureInfo.InvariantCulture)),
                                                },
                                                Value = ((resid.Value - residMean) / residStd).ToString(CultureInfo.InvariantCulture),
                                            })
                                            .ToList(),
                };                
            }

            if (formula == "Fitted")
            {
                var values = new List<GroupingIndexValue>();
                for (int i = 0; i < modelResults.LinearMixedModelResult.ResidualStats.Residuals.Count(); i++)
                {
                    if (!modelResults.LinearMixedModelResult.ResidualStats.FittedValues[i].HasValue ||
                        !modelResults.LinearMixedModelResult.ResidualStats.Residuals[i].HasValue)
                    {
                        continue;
                    }

                    values.Add(new GroupingIndexValue
                    {
                        GroupingVariable = new List<Tuple<string, string>>
                        {
                            new Tuple<string, string>(
                                "RowId",
                                (modelResults.LinearMixedModelResult.ResidualStats.FittedValues[i].Value).ToString(CultureInfo.InvariantCulture)),
                        },
                        Value = ((modelResults.LinearMixedModelResult.ResidualStats.Residuals[i].Value - residMean) / residStd).ToString(CultureInfo.InvariantCulture),

                    });
                }
                return new DataModel
                {
                    ValueList = values,
                };                
            }

            throw new DataException("Residuals support either QQ or Fitted");
        }
    }
}
