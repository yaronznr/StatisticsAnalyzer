using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace StatisticsAnalyzerCore.DataExplore
{
    public class TableStats
    {
        public Dictionary<string, ColumnStats> ColumnStats { get; private set; }
        public TableAnalysis TableAnalysis { get; private set; }
 
        public TableStats(Dictionary<string, ColumnStats> stats, DataTable dataTable)
        {
            ColumnStats = stats;
            TableAnalysis = new DatasetRelationAnalyzer(this, dataTable).AnalyzeDataset();
        }
    }

    public class ColumnStats
    {
        public Dictionary<object, int> ValuesCount { get; private set; }
        public double ValuesAverage { get; set; }
        public double ValuesStd { get; set; }
        public ColumnQuantiles Quantiles { get; set; }

        public ColumnStats(Dictionary<object, int> valuesCount,
                           double valuesAverage,
                           double valuesStd,
                           ColumnQuantiles quantiles)
        {
            ValuesCount = valuesCount;
            ValuesAverage = valuesAverage;
            ValuesStd = valuesStd;
            Quantiles = quantiles;
        }
    }

    public class ColumnQuantiles
    {
        public double Min { get; set; }
        public double Q1 { get; set; }
        public double Q2 { get; set; }
        public double Q3 { get; set; }
        public double Max { get; set; }
    }

    public static class TableManipulations
    {
        private static void ComputeQuantiles(Dictionary<object, int> columnValues,
                                             DataTable dataTable, 
                                             ColumnQuantiles quantiles,
                                             Func<object, double> objectConverter)
        {
            var distinctValues = columnValues.OrderBy(e => e.Key).ToList();
            var totalValueCount = dataTable.Rows.Count;
            quantiles.Min = objectConverter(distinctValues.First().Key);
            quantiles.Max = objectConverter(distinctValues.Last().Key);

            var totalReached = 0;
            var q1Target = totalValueCount / 4.0;
            var q2Target = totalValueCount / 2.0;
            var q3Target = 3 * totalValueCount / 4.0;
            foreach (var distinctValue in distinctValues)
            {
                if (totalReached < q1Target && totalReached >= q1Target - distinctValue.Value) quantiles.Q1 = objectConverter(distinctValue.Key);
                if (totalReached < q2Target && totalReached >= q2Target - distinctValue.Value) quantiles.Q2 = objectConverter(distinctValue.Key);
                if (totalReached < q3Target && totalReached >= q3Target - distinctValue.Value) quantiles.Q3 = objectConverter(distinctValue.Key);
                totalReached += distinctValue.Value;
            }

        }

        public static IList<object> GetColumnValues(DataTable table, string columnName)
        {
            return (from DataRow row in table.Rows select row[columnName]).ToList();
        }

        public static TableStats GetTableStats(DataTable dataTable)
        {
            var columnStats = new Dictionary<string, ColumnStats>();

            foreach (DataColumn column in dataTable.Columns)
            {
                var values = GetColumnValues(dataTable, column.ColumnName).Where(v => v != DBNull.Value).ToList();

                var columnValues = values.
                                   GroupBy(c => c).
                                   ToDictionary(g => g.Key, g => g.Count());

                double average = -1;
                double std = -1;
                var quentiles = new ColumnQuantiles { Min = -1, Q1 = -1, Q2 = -1, Q3 = -1, Max = -1 };
                if (column.DataType == typeof (double))
                {
                    average = values.Average(val => (double)val);
                    std = values.Average(val => Math.Pow((double)val - average, 2));
                    ComputeQuantiles(columnValues, dataTable, quentiles, val => (double)val);
                }
                if (column.DataType == typeof(int))
                {
                    average = values.Average(val => (int)val);
                    std = values.Average(val => Math.Pow((int)val - average, 2));
                    ComputeQuantiles(columnValues, dataTable, quentiles, val => ((int)val)*1.0);
                }

                columnStats.Add(column.ColumnName, new ColumnStats(columnValues, average, std, quentiles));
            }

            return new TableStats(columnStats, dataTable);
        }
    }
}
