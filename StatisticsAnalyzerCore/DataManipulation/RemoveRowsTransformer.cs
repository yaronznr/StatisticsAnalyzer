using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Serialization;

namespace StatisticsAnalyzerCore.DataManipulation
{
    public class RemoveRowsTransformer : DataTransformer
    {
        [XmlElement]
        public string ColumnName { get; set; }
        [XmlElement]
        public HashSet<string> DeleteValues { get; set; }

        public RemoveRowsTransformer() { }
        public RemoveRowsTransformer(string columnName, IEnumerable<string> deleteValues)
        {
            ColumnName = columnName;
            DeleteValues = new HashSet<string>(deleteValues);
        }

        public override void TransformDataTable(DataTable dataTable)
        {
            var rowsToDelete = dataTable.Rows
                                        .Cast<DataRow>()
                                        .Where(dataRow => DeleteValues.Contains(dataRow[ColumnName].ToString()))
                                        .ToList();

            foreach (var dataRow in rowsToDelete)
            {
                dataTable.Rows.Remove(dataRow);
            }
        }
    }
}
