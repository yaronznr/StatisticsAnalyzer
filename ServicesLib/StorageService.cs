using System.Collections.Generic;
using System.Linq;
using AzureCore;
using System.IO;

namespace ServicesLib
{
    public class StorageService
    {
        private readonly FileHelper _fileHelper;
        public StorageService()
        {
            if (ServiceContainer.EnvironmentService().IsLocal)
            {
                _fileHelper = new LocalFileHelper();
            }
            else
            {
                _fileHelper = new AzureHelper();
            }
            
        }

        public string GetPassword(string userName)
        {
            using (var stream = _fileHelper.DownloadBlob(_fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "users",
                userName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }                
            }
        }

        public void SetPassword(string userName, string password)
        {
            _fileHelper.UploadBlob(_fileHelper.DefaultStorageAccount,
                        _fileHelper.DefaultStorageKey,
                        "users",
                        userName,
                        password);
        }

        public void UploadExcel(string userName, string fileName, MemoryStream excelStream)
        {
            _fileHelper.DeleteDirectory(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}", userName, fileName));
            excelStream.Seek(0, SeekOrigin.Begin);
            _fileHelper.UploadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}", userName, fileName),
                excelStream);

            var extension = Path.GetExtension(fileName);
            excelStream.Seek(0, SeekOrigin.Begin);
            _fileHelper.UploadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/blob{1}", userName, extension),
                excelStream);
        }

        public string GetCurrentExcelName(string userName)
        {
            string etag;
            return _fileHelper.GetLastBlobName(_fileHelper.DefaultStorageAccount,
                                               _fileHelper.DefaultStorageKey,
                                               "files",
                                               string.Format("{0}/", userName),
                                               out etag);

        }

        public string GetCurrentExcelName(string userName, out string etag)
        {
            return _fileHelper.GetLastBlobName(_fileHelper.DefaultStorageAccount,
                                               _fileHelper.DefaultStorageKey,
                                               "files",
                                               string.Format("{0}/", userName),
                                               out etag);
        }

        public Stream GetCurrentExcelFile(string userName)
        {
            var stream = _fileHelper.DownloadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}", userName, GetCurrentExcelName(userName)));
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public string GetSheetName(string userName, string fileName)
        {
            using (var sheetStream = _fileHelper.DownloadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}/sheetName", userName, fileName)))
            {
                if (sheetStream != null)
                {
                    using (var textReader = new StreamReader(sheetStream))
                    {
                       return textReader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public void SetSheetName(string userName, string fileName, string sheetName)
        {
            _fileHelper.UploadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}/sheetName", userName, fileName),
                sheetName);
        }

        public int GetModelCount(string userName, string fileName)
        {
            return _fileHelper.GetBlobDirectoryCount(_fileHelper.DefaultStorageAccount,
                                                     _fileHelper.DefaultStorageKey,
                                                     "files",
                                                     string.Format("{0}/{1}/models/", userName, fileName));
        }

        public int AddModelFormula(string userName, string fileName, string formula)
        {
            var modelId = GetModelCount(userName, fileName);

            var storageFormula = GetModelFormula(userName, fileName, modelId);
            if (storageFormula != formula)
            {
                modelId++;
                _fileHelper.UploadBlob(_fileHelper.DefaultStorageAccount,
                                       _fileHelper.DefaultStorageKey,
                                       "files",
                                       string.Format("{0}/{1}/models/{2}/formula", userName, fileName, modelId),
                                       formula);
            }

            return modelId;
        }

        public string GetModelFormula(string userName, string fileName, int modelId)
        {
            using (var stream = _fileHelper.DownloadBlob(_fileHelper.DefaultStorageAccount,
                                                         _fileHelper.DefaultStorageKey,
                                                         "files",
                                                         string.Format("{0}/{1}/models/{2}/formula", userName, fileName, modelId)))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public Stream GetTransformer(string userName, string fileName)
        {
            var transformerStream = _fileHelper.DownloadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}/modelTransformer", userName, fileName));
            {
                if (transformerStream != null)
                {
                    transformerStream.Seek(0, SeekOrigin.Begin);
                    return transformerStream;
                }
            }

            return null;
        }

        public void SetTransformer(string userName, string fileName, MemoryStream transformerStream)
        {
            transformerStream.Seek(0, SeekOrigin.Begin);
            _fileHelper.UploadBlob(
                _fileHelper.DefaultStorageAccount,
                _fileHelper.DefaultStorageKey,
                "files",
                string.Format("{0}/{1}/modelTransformer", userName, fileName),
                transformerStream);
        }

        public int SetModelAnalysis(string userName, string fileName, string scriptLog)
        {
            var modelId = GetModelCount(userName, fileName);
            _fileHelper.UploadBlob(_fileHelper.DefaultStorageAccount,
                                   _fileHelper.DefaultStorageKey,
                                   "files",
                                   string.Format("{0}/{1}/models/{2}/script", userName, fileName, modelId),
                                   scriptLog);

            return modelId;
        }

        public string GetModelAnalysis(string userName, string fileName)
        {
            var modelId = GetModelCount(userName, fileName);
            using (var stream = _fileHelper.DownloadBlob(_fileHelper.DefaultStorageAccount,
                                                         _fileHelper.DefaultStorageKey,
                                                         "files",
                                                         string.Format("{0}/{1}/models/{2}/script", userName, fileName, modelId)))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }                    
                }
            }

            return null;
        }

        public List<KeyValuePair<string, string>> GetSampleList()
        {
            return _fileHelper.ListBlobs(_fileHelper.DefaultStorageAccount,
                                         _fileHelper.DefaultStorageKey,
                                         "samples",
                                         string.Empty)
                              .Select(s => new KeyValuePair<string, string>(s, GetBlobContent("samples", string.Format("{0}desc", s))))
                              .ToList();
        }

        public void CopySample(string sampleName, string userName)
        {
            _fileHelper.DeleteDirectory(_fileHelper.DefaultStorageAccount,
                                        _fileHelper.DefaultStorageKey,
                                        "files",
                                        Path.Combine(userName, sampleName));
            _fileHelper.CopyDirectory(_fileHelper.DefaultStorageAccount,
                                      _fileHelper.DefaultStorageKey,
                                      "samples",
                                      sampleName,
                                      "files",
                                      userName);
        }

        private string GetBlobContent(string container, string blobName)
        {
            using (var stream = _fileHelper.DownloadBlob(_fileHelper.DefaultStorageAccount,
                                                         _fileHelper.DefaultStorageKey,
                                                         container,
                                                         blobName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;            
        }
    }
}
