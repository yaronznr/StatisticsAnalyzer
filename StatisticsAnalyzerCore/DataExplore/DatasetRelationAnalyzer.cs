using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.DataExplore
{
    public class DatasetRelationAnalyzer
    {
        private readonly TableStats _tableStats;
        private readonly DataTable _dataTable;

        private  Dictionary<string, ColumnClassification> _columnClassifications;
        private Dictionary<string, ColumnBalanace> _columnBalanaces;
        private Dictionary<string, HashSet<string>> _crossBalanaceGroup;
        private Dictionary<string, ColumnRepeated> _columnRepeated;
        private Dictionary<string, Dictionary<string, ColumnRelation>> _columnGraph;

        private int ComputeQuantile(ColumnQuantiles quantiles, object value)
        {
            double dValue = (value is double) ? (double)value : 1.0*(int)value;
            if (dValue < quantiles.Q1) return 0;
            if (dValue > quantiles.Q3) return 3;
            if (dValue > quantiles.Q2) return 2;
            return 1;
        }

        private bool CheckRatio(double a, double b, double maxRatio, double maxGap)
        {
            if ((a>b && a/b < maxRatio) || (b>a && b/a < maxRatio)) return true;
            if (Math.Abs(a - b) < maxGap) return true;
            return false;
        }

        private void ComputeColumnClassification()
        {
            var tableLength = _dataTable.Rows.Count;
            foreach (var columnStat in _tableStats.ColumnStats)
            {
                var columnName = columnStat.Key;
                if (_dataTable.Columns[columnName].DataType == typeof(string))
                {
                    if (columnStat.Value.ValuesCount.Count == tableLength)
                    {
                        _columnClassifications[columnName] = ColumnClassification.UniqueId;
                    }
                    else if (columnStat.Value.ValuesCount.Count == 1)
                    {
                        _columnClassifications[columnName] = ColumnClassification.SingleValue;
                    }
                    else
                    {
                        _columnClassifications[columnName] = ColumnClassification.Grouping;
                    }
                }
                else
                {
                    if (columnStat.Value.ValuesCount.Count == tableLength &&
                        _dataTable.Columns[columnName].DataType == typeof(int) &&
                        tableLength > (columnStat.Value.Quantiles.Max - columnStat.Value.Quantiles.Min)/2) // Detect running integers column
                    {
                        _columnClassifications[columnName] = ColumnClassification.UniqueId;
                    }
                    else
                    {
                        _columnClassifications[columnName] = ColumnClassification.Measure;                        
                    }
                }
            }            
        }
        private void ComputeColumnBalance()
        {
            var tableLength = _dataTable.Rows.Count;
            foreach (var columnStat in _tableStats.ColumnStats)
            {
                var columnName = columnStat.Key;
                if (_dataTable.Columns[columnName].DataType == typeof(string) &&
                    columnStat.Value.ValuesCount.Count != tableLength &&
                    columnStat.Value.ValuesCount.Count != 1)
                {
                    _columnClassifications[columnName] = ColumnClassification.Grouping;

                    var representativeValue = columnStat.Value.ValuesCount.First().Value;
                    if (columnStat.Value.ValuesCount.All(cnt => cnt.Value == representativeValue))
                    {
                        _columnBalanaces[columnName] = ColumnBalanace.Balanced;
                    }
                    else if (columnStat.Value.ValuesCount.All(cnt => CheckRatio(cnt.Value,
                                                                                columnStat.Value.ValuesCount.Average(e => e.Value),
                                                                                1.2,
                                                                                1.5)))
                    {
                        _columnBalanaces[columnName] = ColumnBalanace.SemiBalanced;
                    }
                    else
                    {
                        _columnBalanaces[columnName] = ColumnBalanace.NonBalanaced;
                    }
                }
            }
        }
        private void ComputeBalanceGroups()
        {
            foreach (DataColumn column in _dataTable.Columns)
            {
                if (_columnBalanaces.ContainsKey(column.ColumnName) &&
                    _columnBalanaces[column.ColumnName] == ColumnBalanace.Balanced)
                {
                    _crossBalanaceGroup.Add(column.ColumnName, new HashSet<string> {column.ColumnName});
                }
            }

            foreach (var inspectedColumn in _crossBalanaceGroup.Keys)
            {
                foreach (var toMergeColumn in _crossBalanaceGroup.Keys)
                {
                    if (toMergeColumn != inspectedColumn)
                    {
                        if (CheckBalance(_crossBalanaceGroup[inspectedColumn], toMergeColumn))
                        {
                            _crossBalanaceGroup[inspectedColumn].Add(toMergeColumn);
                        }
                    }
                }
            }
        }
        private void ComputeColumnRepeated()
        {
            foreach (DataColumn dataColumn in _dataTable.Columns)
            {
                CheckRepeated(dataColumn);
            }
        }

        private void ComputeColumnCharacteristics()
        {
            ComputeColumnClassification();
            ComputeColumnBalance();
            ComputeBalanceGroups();
            ComputeColumnRepeated();
        }

        private void CheckRepeated(DataColumn dataColumn)
        {
            var columnValueCounts = _tableStats.ColumnStats[dataColumn.ColumnName].ValuesCount;
            if (columnValueCounts.All(e => e.Value > 1))
            {
                _columnRepeated[dataColumn.ColumnName] = ColumnRepeated.StrictRepeated;
            }
            else if (columnValueCounts.Count(e => e.Value > 1) > 0.6 * columnValueCounts.Count)
            {
                _columnRepeated[dataColumn.ColumnName] = ColumnRepeated.StatisticRepeated;
            }
            else
            {
                _columnRepeated[dataColumn.ColumnName] = ColumnRepeated.NonRepeated;
            }
        }
        private bool CheckNested(DataColumn column1, DataColumn column2)
        {
            if (_columnClassifications[column1.ColumnName] != ColumnClassification.Grouping ||
                _columnClassifications[column2.ColumnName] != ColumnClassification.Grouping)
            {
                return false;
            }

            var d = new Dictionary<string, HashSet<string>>();
            foreach (DataRow dataRow in _dataTable.Rows)
            {
                if (dataRow[column1] != null && dataRow[column2] != null)
                {
                    var c1Value = dataRow[column1].ToString();
                    var c2Value = dataRow[column2].ToString();

                    if (!string.IsNullOrEmpty(c2Value.Trim()))
                    {
                        if (!d.ContainsKey(c1Value))
                        {
                            d[c1Value] = new HashSet<string>();
                        }

                        d[c1Value].Add(c2Value);
                    }
                }
            }

            if (d.Values.All(e => e.Count == 1))
            {
                return true;
            }

            return false;
        }
        private bool CheckCovering(DataColumn column1, DataColumn column2, out LinkAttributes linkAttribute)
        {
            if (_columnClassifications[column1.ColumnName] != ColumnClassification.Grouping)
            {
                linkAttribute = LinkAttributes.None; 
                return false;
            }

            var d = new Dictionary<string, Dictionary<string, int>>();
            foreach (DataRow dataRow in _dataTable.Rows)
            {
                if (dataRow[column1] != null && dataRow[column2] != null)
                {
                    var c1Value = dataRow[column1].ToString();
                    var c2Value = dataRow[column2].ToString();

                    if (!string.IsNullOrEmpty(c2Value.Trim()))
                    {
                        if (!d.ContainsKey(c1Value))
                        {
                            d[c1Value] = new Dictionary<string, int>();
                        }

                        if (!d[c1Value].ContainsKey(c2Value))
                        {
                            d[c1Value][c2Value] = 0;
                        }
                            
                        d[c1Value][c2Value]++;                            
                    }
                }
            }

            var c2TotalValueCount = _tableStats.ColumnStats[column2.ColumnName].ValuesCount.Count;
            if (d.Values.All(e => e.Count == c2TotalValueCount))
            {
                linkAttribute = LinkAttributes.StrictCovering;
                return true;
            }

            var cnt = d.Values.Max(e => e.Count);
            if (cnt > 1 && _columnClassifications[column2.ColumnName] == ColumnClassification.Grouping)
            {
                if (d.Values.Count(e => CheckRatio(e.Count, c2TotalValueCount, 1.2, 2.0)) > 0.5 * d.Values.Count)
                {
                    linkAttribute = LinkAttributes.StatisticCovering;
                    return true;
                }
            }
            else if (_columnClassifications[column2.ColumnName] != ColumnClassification.Grouping)
            {
                var q = new Dictionary<string, Dictionary<int, int>>();
                foreach (DataRow dataRow in _dataTable.Rows)
                {
                    if (dataRow[column1] != null && dataRow[column2] != null &&
                        dataRow[column1] != DBNull.Value && dataRow[column2] != DBNull.Value)
                    {
                        var c1Value = dataRow[column1].ToString();
                        var c2Value = ComputeQuantile(_tableStats.ColumnStats[column2.ColumnName].Quantiles, dataRow[column2]);

                        if (!q.ContainsKey(c1Value))
                        {
                            q[c1Value] = new Dictionary<int, int>();
                        }

                        if (!q[c1Value].ContainsKey(c2Value))
                        {
                            q[c1Value][c2Value] = 0;
                        }

                        q[c1Value][c2Value]++;
                    }
                }

                if (q.Values.Count(e => CheckRatio(e.Count, 4, 1.8, 1.5)) > 0.5 * d.Values.Count)
                {
                    linkAttribute = LinkAttributes.StatisticCovering;
                    return true;
                }
            }

            linkAttribute = LinkAttributes.None;
            return false;
        }
        private bool CheckBalance(HashSet<string> columnGroup, string toMergeColumn)
        {
            var groupCounts = new Dictionary<ValueGroupIndex, int>();
            foreach (DataRow dataRow in _dataTable.Rows)
            {
                var row = dataRow;
                var valueGroup = new ValueGroupIndex(columnGroup.Select(cl => row[cl].ToString())
                                                                .Concat(new[] { row[toMergeColumn].ToString() }));
                if (!groupCounts.ContainsKey(valueGroup))
                {
                    groupCounts[valueGroup] = 0;
                }

                groupCounts[valueGroup]++;
            }

            return groupCounts.Values.All(val => val == groupCounts.Values.First());
        }

        private void AddColumnRelation(DataColumn column1,
                                       DataColumn column2,
                                       LinkAttributes linkAttribute)
        {
            if (!_columnGraph.ContainsKey(column1.ColumnName))
            {
                _columnGraph[column1.ColumnName] = new Dictionary<string, ColumnRelation>();
            }
            if (!_columnGraph[column1.ColumnName].ContainsKey(column2.ColumnName))
            {
                _columnGraph[column1.ColumnName][column2.ColumnName] = new ColumnRelation();
            }

            _columnGraph[column1.ColumnName][column2.ColumnName].RelationAttributes.Add(linkAttribute);
        }

        public TableAnalysis AnalyzeDataset()
        {
            _columnGraph = new Dictionary<string, Dictionary<string, ColumnRelation>>();
            _columnBalanaces = new Dictionary<string, ColumnBalanace>();
            _crossBalanaceGroup = new Dictionary<string, HashSet<string>>();
            _columnRepeated = new Dictionary<string, ColumnRepeated>();
            _columnClassifications = new Dictionary<string, ColumnClassification>();
            bool isGraphComputed = false;

            ComputeColumnCharacteristics();

            if (_dataTable.Columns.Count <= 1000)
            {
                isGraphComputed = true;
                foreach (DataColumn column1 in _dataTable.Columns)
                {
                    foreach (DataColumn column2 in _dataTable.Columns)
                    {
                        // No self relations
                        if (column1 == column2) continue;

                        // No relations areassociated with null columns
                        if (_columnClassifications[column1.ColumnName] == ColumnClassification.UniqueId ||
                            _columnClassifications[column2.ColumnName] == ColumnClassification.UniqueId) continue;

                        if (CheckNested(column1, column2))
                        {
                            AddColumnRelation(column1, column2, LinkAttributes.Nested);
                        }

                        LinkAttributes linkAttribute;
                        if (CheckCovering(column1, column2, out linkAttribute))
                        {
                            AddColumnRelation(column1, column2, linkAttribute);
                        }
                    }
                }
            }

            return new TableAnalysis(_columnClassifications,
                                     _columnBalanaces,
                                     _crossBalanaceGroup,
                                     _columnRepeated,
                                     _columnGraph,
                                     isGraphComputed);
        }

        public DatasetRelationAnalyzer(TableStats tableStats, 
                                       DataTable dataTable)
        {
            _tableStats = tableStats;
            _dataTable = dataTable;
        }
    }
}
