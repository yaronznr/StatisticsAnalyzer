using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;

namespace StatisticsAnalyzerCore.Modeling
{
    public static class ModelGenerator
    {
        // ReSharper disable once UnusedParameter.Local
        private static List<string> GetPredictedFixedEffects(ModelDataset dataset,
                                                             string predictedVariable,
                                                             List<string> remainingCols)
        {
            var excludedList = new HashSet<string>();
            var fixedList = new List<string>();
            foreach (var column in remainingCols.OrderBy(col => (dataset.TableStats.TableAnalysis.ColumnBalanaces.ContainsKey(col) &&
                                                                 dataset.TableStats.TableAnalysis.ColumnBalanaces[col] != ColumnBalanace.Balanced)
                                                                 ? 0 : 1)
                                                .ThenBy(c => dataset.TableStats.ColumnStats[c].ValuesCount.Count)
                                                .Where(col => dataset.TableStats.TableAnalysis.ColumnClassifications.ContainsKey(col) &&
                                                              dataset.TableStats.TableAnalysis.ColumnClassifications[col] != ColumnClassification.UniqueId)                                
                                                .Take(5))
            {
                if (excludedList.Contains(column)) continue;

                fixedList.Add(column);

                foreach (var colRel in dataset.TableStats.TableAnalysis.ColumnGraph)
                {
                    if (colRel.Value.ContainsKey(column))
                    {
                            foreach (var att in colRel.Value[column].RelationAttributes)
                            {
                                if (att == LinkAttributes.Nested)
                                {
                                    excludedList.Add(colRel.Key);
                                }
                            }
                    }
                }
            }
            return fixedList;
        }

        private static List<string> GetPredictedRandomEffects(
            ModelDataset dataset,
            List<string> fixedEffects,
            List<string> remainingCols)
        {
            var randomEffects = new List<string>();
            foreach (var col in remainingCols)
            {
                if (dataset.TableStats.TableAnalysis.ColumnGraph.ContainsKey(col))
                {
                    var colRels = dataset.TableStats.TableAnalysis.ColumnGraph[col];
                    foreach (var columnRelation in colRels)
                    {
                        if (fixedEffects.Contains(columnRelation.Key) &&
                            columnRelation.Value.RelationAttributes.Contains(LinkAttributes.Nested) &&
                            dataset.TableStats.TableAnalysis.ColumnRepeated.ContainsKey(col) &&
                            dataset.TableStats.TableAnalysis.ColumnRepeated[col] != ColumnRepeated.NonRepeated &&
                            dataset.TableStats.ColumnStats[col].ValuesCount.Count >= 5)
                        {
                            randomEffects.Add(col);
                        }
                    }
                }
            }

            return randomEffects.Distinct().ToList();
        }

        public static MixedLinearModel PerdictModel(ModelDataset dataset)
        {
            var dataTable = dataset.DataTable;
            var columns = new List<DataColumn>();
            foreach (DataColumn column in dataTable.Columns)
            {
                columns.Add(column); 
            }

            var orderedCols = columns.OrderBy(col => col.DataType != typeof(String) ? 0:2).Select(c => c.ColumnName).ToList();
            var predictedVariable = orderedCols.First();
            var fixedEffects = GetPredictedFixedEffects(dataset, predictedVariable, orderedCols.Skip(1).ToList());
            var randomEffects = GetPredictedRandomEffects(dataset, fixedEffects, orderedCols.Skip(1).Where(e => !fixedEffects.Contains(e)).ToList());
            return new MixedLinearModel(fixedEffects, randomEffects, predictedVariable);
        }
    }
}
