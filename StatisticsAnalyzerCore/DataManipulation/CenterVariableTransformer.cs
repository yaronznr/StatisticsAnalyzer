using System;
using System.Data;
using System.Linq;
using System.Xml.Serialization;
using StatisticsAnalyzerCore.Helper;

namespace StatisticsAnalyzerCore.DataManipulation
{
    [Serializable]
    public class CenterVariableTransformer : DataTransformer
    {
        [XmlElement]
        public string ColumnName { get; set; }

        public override void TransformDataTable(DataTable dataTable)
        {
            var meanColumnSum = dataTable.Rows.Cast<DataRow>()
                                              .Select(r => r[ColumnName])
                                              .Where(v => v != DBNull.Value && v != null)
                                              .Average(v => v.ConvertDouble());

            var type = dataTable.Columns[ColumnName].DataType;
            foreach (DataRow dataRow in dataTable.Rows.Cast<DataRow>()
                                                      .Where(r => r[ColumnName] != DBNull.Value && r[ColumnName] != null))
            {
                var value = dataRow[ColumnName].ConvertDouble() - meanColumnSum;
                dataRow[ColumnName] = type == typeof(double) ? value : (int)(value);
            }
        }
    }
}
