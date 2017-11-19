using System;
using System.Data;
using System.Xml.Serialization;

namespace StatisticsAnalyzerCore.DataManipulation
{
    [Serializable]
    public class LogTransformer : DataTransformer
    {
        [XmlElement]
        public string ColumnName { get; set; }

        public LogTransformer() {}
        public LogTransformer(string columnName)
        {
            ColumnName = columnName;
        }

        public override void TransformDataTable(DataTable dataTable)
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                dataRow[ColumnName] = Math.Log((double)dataRow[ColumnName]);
            }
        }
    }
}
