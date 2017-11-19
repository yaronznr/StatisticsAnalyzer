using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ExcelLib;
using System.Collections.Generic;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.DataManipulation;

namespace ServicesLib
{
    public class ExcelService
    {
        private Dictionary<string, ModelDataset> userTables = new Dictionary<string, ModelDataset>();
        private void AddUserTable(string etag, ModelDataset modelDataset)
        {
            lock (userTables)
            {
                if (userTables.ContainsKey(etag))
                {
                    userTables[etag] = modelDataset;
                }
                else
                {
                    userTables.Add(etag, modelDataset);
                }
            }
        }

        private void PurgeUserTable(string etag)
        {
            lock (userTables)
            {
                userTables.Remove(etag);
            }
        }

        public ModelDataset GetExcelDocument(string userName)
        {
            string etag;
            ModelDataset modelDataset = null;
            var serializer = new XmlSerializer(typeof(DataTransformer));

            var lastBlobName = ServiceContainer.StorageService().GetCurrentExcelName(userName, out etag);
            if (lastBlobName != null)
            {
                lock (userTables)
                {
                    userTables.TryGetValue(etag, out modelDataset);
                }

                // Read table from memory if not in cache
                if (modelDataset == null)
                {
                    DataTable dataTable;
                    var ext = Path.GetExtension(lastBlobName);
                    if (ext == ".xlsx")
                    {
                        using (var excelStream = ServiceContainer.StorageService().GetCurrentExcelFile(userName))
                        {
                            string sheetName = ServiceContainer.StorageService().GetSheetName(userName, lastBlobName);
                            var excelFile = new ExcelDocument(excelStream, sheetName);
                            dataTable = excelFile.LoadCellData();
                            if (sheetName == null)
                            {
                                ServiceContainer.StorageService().SetSheetName(userName, lastBlobName, excelFile.SheetName);
                            }
                        }
                    }
                    else if (ext == ".csv")
                    {
                        using (var excelStream = ServiceContainer.StorageService().GetCurrentExcelFile(userName))
                        {
                            var csvFile = new CsvDocument(excelStream);
                            dataTable = csvFile.LoadCellData();                            
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown file type");
                    }

                    DataTransformer transformer = new CompositeDataTransformer(new List<DataTransformer>());

                    /*var transformerList = (CompositeDataTransformer)transformer;
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (column.DataType == typeof(double) ||
                            column.DataType == typeof(int))
                        {
                            transformerList.Transformers.Add(new CenterVariableTransformer
                            {
                                ColumnName = column.ColumnName,
                            });                                                    
                        }
                    }*/

                    using (var transformerStream = ServiceContainer.StorageService().GetTransformer(userName, lastBlobName))
                    {
                        if (transformerStream != null)
                        {
                            transformer = (DataTransformer) serializer.Deserialize(transformerStream);
                        }
                    }
                    transformer.TransformDataTable(dataTable);
                    modelDataset = new ModelDataset
                    {
                        DataTable = dataTable,
                        TableStats = TableManipulations.GetTableStats(dataTable),
                        DataTransformer = transformer,
                    };

                    lock (userTables)
                    {
                        AddUserTable(etag, modelDataset);
                    }
                }
            }

            return modelDataset;
        }

        public void AddDataTransformer(string userName, DataTransformer transformer)
        {
            var serializer = new XmlSerializer(typeof(DataTransformer));

            string etag;
            var lastBlobName = ServiceContainer.StorageService().GetCurrentExcelName(userName, out etag);
            if (lastBlobName != null)
            {
                CompositeDataTransformer prevTransformer;
                if (userTables.ContainsKey(etag))
                {
                    prevTransformer = (CompositeDataTransformer)userTables[etag].DataTransformer;
                }
                else
                {
                    using (var prevTransformerStream = ServiceContainer.StorageService().GetTransformer(userName, lastBlobName))
                    {
                        if (prevTransformerStream != null)
                        {
                            prevTransformer = (CompositeDataTransformer)serializer.Deserialize(prevTransformerStream);
                        }
                        else
                        {
                            prevTransformer = new CompositeDataTransformer(new List<DataTransformer>());
                        }                                            
                    }
                }

                prevTransformer.Transformers.Add(transformer);

                var memStream = new MemoryStream();
                serializer.Serialize(memStream, prevTransformer);
                memStream.Seek(0, SeekOrigin.Begin);
                ServiceContainer.StorageService().SetTransformer(userName, lastBlobName, memStream);

                PurgeUserTable(etag); // Purge cache so new transformer will be computed
                return;
            }

            throw new Exception("Transformer with no excel file");
        }

        public void RemoveDataTransformer(string userName, string transformerId)
        {
            var serializer = new XmlSerializer(typeof(DataTransformer));

            string etag;
            var lastBlobName = ServiceContainer.StorageService().GetCurrentExcelName(userName, out etag);
            if (lastBlobName != null)
            {
                CompositeDataTransformer prevTransformer;
                if (userTables.ContainsKey(etag))
                {
                    prevTransformer = (CompositeDataTransformer)userTables[etag].DataTransformer;
                }
                else
                {
                    using (var prevTransformerStream = ServiceContainer.StorageService().GetTransformer(userName, lastBlobName))
                    {
                        if (prevTransformerStream != null)
                        {
                            prevTransformer = (CompositeDataTransformer)serializer.Deserialize(prevTransformerStream);
                        }
                        else
                        {
                            prevTransformer = new CompositeDataTransformer(new List<DataTransformer>());
                        }
                    }
                }

                prevTransformer.Transformers = prevTransformer
                                              .Transformers
                                              .Where(t => t.TransformerId != transformerId)
                                              .ToList();

                var memStream = new MemoryStream();
                serializer.Serialize(memStream, prevTransformer);
                memStream.Seek(0, SeekOrigin.Begin);
                ServiceContainer.StorageService().SetTransformer(userName, lastBlobName, memStream);

                PurgeUserTable(etag); // Purge cache so new transformer will be computed
                return;
            }

            throw new Exception("Transformer with no excel file");
        }

    }
}
