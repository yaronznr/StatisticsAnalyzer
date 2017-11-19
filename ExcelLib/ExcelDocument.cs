using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Data;
using System.IO;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;
using DataTable = System.Data.DataTable;
using NumberingFormat = DocumentFormat.OpenXml.Spreadsheet.NumberingFormat;

namespace ExcelLib
{
    public abstract class DataDocument
    {
        private static double ConvertDouble(object val)
        {
            if (val is int)
            {
                return ((int)val) * 1.0;
            }

            return (double)val;
        }
        private static object ConvertString(string str, Type toType)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            if (toType == typeof (string))
            {
                return str;
            }

            if (toType == typeof(double))
            {
                return Double.Parse(str);
            }

            return Int32.Parse(str);
        }
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {

            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }
            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }
                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
            shareStringPart.SharedStringTable.Save();
            return i;
        }
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, Dictionary<uint, Row> rowCache, WorksheetPart worksheetPart)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            var sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (rowCache.ContainsKey(rowIndex))
            {
                row = rowCache[rowIndex];
            }
            /*else if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
                rowCache[rowIndex] = row;
            }*/
            else
            {
                row = new Row { RowIndex = rowIndex };
                // ReSharper disable PossiblyMistakenUseOfParamsMethod
                sheetData.Append(row);
                // ReSharper restore PossiblyMistakenUseOfParamsMethod
                rowCache[rowIndex] = row;
            }

            // If there is not a cell with the specified column name, insert one. 
            if (row.Elements<Cell>().Any(c => c.CellReference.Value == columnName + rowIndex))
            {
                return row.Elements<Cell>().First(c => c.CellReference.Value == cellReference);
            }
            // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
            var newCell = new Cell { CellReference = cellReference };
            row.InsertBefore(newCell, null);
            return newCell;
        }
        private static Stylesheet CreateStylesheet()
        {
            var ss = new Stylesheet();

            var nfs = new NumberingFormats();
            var nformatDateTime = new NumberingFormat
            {
                NumberFormatId = UInt32Value.FromUInt32(1),
                FormatCode = StringValue.FromString("dd/mm/yyyy")
            };
            // ReSharper disable PossiblyMistakenUseOfParamsMethod
            nfs.Append(nformatDateTime);
            ss.Append(nfs);
            // ReSharper restore PossiblyMistakenUseOfParamsMethod

            return ss;
        }
        protected static string CreateSafeColumnName(string rawColumnName, HashSet<string> columnNames)
        {
            var suggestedColumnName = Regex.Replace(rawColumnName, @"[^a-zA-Z0-9 ]", string.Empty).Replace(" ", "_");
            if (columnNames.Contains(suggestedColumnName))
            {
                int i = 2;
                while (columnNames.Contains(string.Format("{0}_{1}", suggestedColumnName, i))) i++;
                suggestedColumnName = string.Format("{0}_{1}", suggestedColumnName, i);
            }
            
            columnNames.Add(suggestedColumnName);
            return suggestedColumnName;
        }
        public static void EncodeDataTable(Stream stream,
                                           ModelDataset dataset,
                                           List<string> selectedColumns, 
                                           ICollection<string> nonCenterColumns,
                                           Dictionary<string, string> variableNameReplacement)
        {
            var rowCache = new Dictionary<uint, Row>();
            var dataTable = dataset.DataTable;
            var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            // Add a WorkbookPart to the document.
            var workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();
            var wbsp = workbookpart.AddNewPart<WorkbookStylesPart>();
            wbsp.Stylesheet = CreateStylesheet();
            wbsp.Stylesheet.Save();

            // Add a WorksheetPart to the WorkbookPart.
            var worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.
            var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());

            // Append a new worksheet and associate it with the workbook.
            var sheet = new Sheet
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "mySheet"
            };

            // ReSharper disable PossiblyMistakenUseOfParamsMethod
            sheets.Append(sheet);
            // ReSharper restore PossiblyMistakenUseOfParamsMethod

            var columnIndices = new Dictionary<string, int>();
            for (var idx = 0; idx < dataTable.Columns.Count; idx++)
            {
                columnIndices.Add(dataTable.Columns[idx].ColumnName, idx);
            }

            uint row = 2;
            foreach (DataRow dr in dataTable.Rows)
            {
                var newXlColumn = 0;
                foreach (var selectedColumn in selectedColumns)
                {
                    if (dr[selectedColumn] == DBNull.Value)
                    {
                        newXlColumn++;
                        continue;
                    }

                    var idx = columnIndices[selectedColumn];

                    string cl;
                    if (newXlColumn >= 26)
                        cl = Convert.ToString(Convert.ToChar(65 + (newXlColumn - 26) / 26)) + Convert.ToString(Convert.ToChar(65 + (newXlColumn - 26) % 26));
                    else
                        cl = Convert.ToString(Convert.ToChar(65 + newXlColumn));
                    newXlColumn++;
                    Cell cell;
                    if (row == 2)
                    {
                        //index = InsertSharedStringItem(dataTable.Columns[idx].ColumnName, shareStringPart);
                        cell = InsertCellInWorksheet(cl, row - 1, rowCache, worksheetPart);
                        cell.CellValue = new CellValue(variableNameReplacement[dataTable.Columns[idx].ColumnName]);
                        cell.DataType = new EnumValue<CellValues>(CellValues.InlineString);
                    }

                    // Insert the text into the SharedStringTablePart.
                    //index = InsertSharedStringItem(Convert.ToString(dr[selectedColumn]), shareStringPart);

                    var offset = nonCenterColumns.Contains(selectedColumn)
                        ? 0
                        : - dataset.TableStats.ColumnStats[selectedColumn].ValuesAverage;
                    cell = InsertCellInWorksheet(cl, row, rowCache, worksheetPart);
                    cell.CellValue = new CellValue(Convert.ToString(dataTable.Columns[idx].DataType == typeof(string) ?
                                                                        dr[selectedColumn] :
                                                                        ConvertDouble(dr[selectedColumn]) + offset));
                    cell.DataType = new EnumValue<CellValues>(
                        (dataTable.Columns[idx].DataType == typeof(string)) ?
                            CellValues.InlineString :
                            CellValues.Number
                        );
                }
                row++;
            }

            // Close the document.
            worksheetPart.Worksheet.Save();
            workbookpart.Workbook.Save();
            spreadsheetDocument.Close();
        }

        // detect factor levels, covert int/float values, handle dates
        protected DataTable TransformDataTable(Dictionary<string, List<string>> dataTable)
        {
            var sampleCount = dataTable.First().Value.Count;
            int a; double b;
            if (dataTable.Any(x => x.Value.Count != sampleCount) && 
                dataTable.Keys.All(k => dataTable[k].All(v => v != null)) &&
                dataTable.Keys.All(k => dataTable[k].All(v => double.TryParse(v, out b))))
            {
                var newDict = new Dictionary<string, List<string>>();
                newDict["group"] = new List<string>();
                newDict["value"] = new List<string>();

                foreach (var key in dataTable.Keys)
                {
                    foreach (var val in dataTable[key])
                    {
                        newDict["group"].Add(key);
                        newDict["value"].Add(val);
                    }
                }

                dataTable = newDict;
            }

            dataTable["uniqueid"] = new List<string>();
            for (int i = 0; i < dataTable.First().Value.Count; i++)
            {
                dataTable["uniqueid"].Add(i.ToString(CultureInfo.InvariantCulture));
            }

            var d = new Dictionary<string, Type>();
            var columnNames = dataTable.Keys;
            foreach (string columnName in columnNames.Where(str => !string.IsNullOrEmpty(str)))
            {
                var columnValues = dataTable[columnName];

                // Currently with less than 5 distinct values. We treat this variable as 
                // discrete as it makes more sense than fit a line for 4 or less x values
                if (columnValues.Distinct().Count() < 5)
                {
                    d[columnName] = typeof(string);
                    continue;
                }

                if (columnValues.All(val => string.IsNullOrEmpty(val) || Int32.TryParse(val.ToString(CultureInfo.InvariantCulture), out a)))
                {
                    d[columnName] = typeof (int);
                }
                else if (columnValues.All(val => string.IsNullOrEmpty(val) || Double.TryParse(val.ToString(CultureInfo.InvariantCulture), out b)))
                {
                    d[columnName] = typeof (double);
                }
                else
                {
                    d[columnName] = typeof (string);
                }
            }

            var newDt = new DataTable();
            foreach (string columnName in dataTable.Keys.Where(str => !string.IsNullOrEmpty(str)))
            {
                newDt.Columns.Add(columnName, d[columnName]);
            }
            for (int i = 0; i < dataTable.Max(x => x.Value.Count); i++)
            {
                newDt.Rows.Add(dataTable.Select(kvp => ConvertString(kvp.Value.Count > i ? kvp.Value[i] : null, d[kvp.Key]))
                                        .ToArray());
            }

            return newDt;
        }

        protected readonly Stream Stream;
        public DataDocument(Stream stream)
        {
            Stream = stream;
        }

        public abstract DataTable LoadCellData();
    }
    public class ExcelDocument : DataDocument
    {
        #region Private Members
        
        /// <summary>
        /// The excel document
        /// </summary>
        private SpreadsheetDocument _excelDocument;

        #endregion

        public string SheetName { get; private set; }


        /// <summary>
        /// Initialize an <see cref="ExcelDocument"/>
        /// </summary>
        /// <param name="stream">The stream of the excel document</param>
        /// <param name="sheetName">The sheet name</param>
        public ExcelDocument(Stream stream, string sheetName) : base(stream)
        {
            _excelDocument = SpreadsheetDocument.Open(Stream, false);
            SheetName = sheetName;
        }

        private string GetExcelColumnName(string cellName)
        {
            var d2 = cellName[1] - '1';
            return cellName.Substring(0, (d2 >= 0 && d2 < 9) ? 1 : 2);
        }

        public static List<string> GetSheetsList(Stream stream)
        {
            return SpreadsheetDocument.Open(stream, false)
                                      .WorkbookPart
                                      .Workbook
                                      .Descendants<Sheet>()
                                      .Select(s => s.Name.Value)
                                      .ToList();
        }

        /// <summary>
        /// Load cell data into a dataset
        /// </summary>
        public override DataTable LoadCellData()
        {
            WorkbookPart workbook = _excelDocument.WorkbookPart;
            Sheet selectedSheet;
            if (SheetName == null)
            {
                selectedSheet = _excelDocument.WorkbookPart.Workbook.Descendants<Sheet>().First();
                SheetName = selectedSheet.Name;
            }
            else
            {
                selectedSheet = _excelDocument.WorkbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(sht => sht.Name == SheetName);
            }

            if (selectedSheet == null)
            {
                selectedSheet = _excelDocument.WorkbookPart.Workbook.Descendants<Sheet>().First();
                SheetName = selectedSheet.Name;
            }

            // For shared strings, look up the value in the shared 
            // strings table.
            var stringTable = workbook.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();

            var columnNames = new HashSet<string>();
            var selectedWorksheetPart = (WorksheetPart)workbook.GetPartById(selectedSheet.Id);
            var partitionedcells = selectedWorksheetPart.Worksheet
                .Descendants<Cell>()
                .GroupBy(cell => GetExcelColumnName(cell.CellReference.Value))
                .ToDictionary(group => CreateSafeColumnName(group.First().CellValueAsString(stringTable), columnNames),
                              group => group.Skip(1)
                                            .Select(e => e.CellValueAsString(stringTable))
                                            .ToList());

            return TransformDataTable(partitionedcells);
        }

    }
    public class CsvDocument : DataDocument
    {
        public CsvDocument(Stream stream) : base(stream)
        {
        }

        public override DataTable LoadCellData()
        {
            var columnNamesSet = new HashSet<string>();
            using (var reader = new StreamReader(Stream))
            {
                var headerLine = reader.ReadLine();
                if (headerLine != null)
                {
                    var columnNames = headerLine.Split(',');

                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        columnNames[i] = CreateSafeColumnName(columnNames[i], columnNamesSet);
                    }
                    var dataTable = columnNames.ToDictionary(c => c, c => new List<string>());

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var dataLine = line.Split(',');
                            for (int i = 0; i < columnNames.Length; i++)
                            {
                                dataTable[columnNames[i]].Add(dataLine[i]);
                            }                        
                        }
                    }

                    return TransformDataTable(dataTable);
                }

                throw new Exception("File is empty");
            }
        }
    }
}
