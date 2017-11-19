using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Globalization;
using System.Linq;

namespace ExcelLib
{
    public static class OpenXmlHelper
    {
        public static string CellValueAsString(this Cell cell, SharedStringTablePart sharedStringTable = null)
        {
            var cellValue = cell.CellValue;
            var dataType = cell.DataType != null ? cell.DataType.Value : CellValues.String;

            if (dataType == CellValues.Boolean ||
                dataType == CellValues.Date ||
                dataType == CellValues.Error ||
                dataType == CellValues.InlineString ||
                dataType == CellValues.Number ||
                dataType == CellValues.String)
            {
                if (cellValue == null) return null;
                return cellValue.InnerText;
            }

            if (dataType == CellValues.SharedString)
            {
                // If the shared string table is missing, something is 
                // wrong. Return the index that you found in the cell.
                // Otherwise, look up the correct text in the table.
                if (sharedStringTable != null)
                {
                    return sharedStringTable.SharedStringTable.
                        ElementAt(int.Parse(cellValue.InnerText)).InnerText;
                }

                return string.Format(CultureInfo.InvariantCulture, "String:{0}", cellValue.InnerText);
            }

            throw new Exception("Error");
        }
    }
}
