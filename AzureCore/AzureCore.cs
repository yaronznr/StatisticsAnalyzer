using System.Collections.Generic;
using System.Globalization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;

namespace AzureCore
{
    public abstract class FileHelper
    {
        public string DefaultStorageAccount = "mixedmodel";
        public string DefaultStorageKey = "m3Oc6Y9BXOADjluwo82O0HLXTa3RV9x8sD+pONXXXHU3DPuqPt5DXZaYv9Of9iagQuZD8upxBIow245PN3cOEA==";

        public abstract void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            Stream stream);

        public abstract void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            string content);

        public abstract Stream DownloadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName);

        public abstract void DeleteBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName);

        public abstract string GetLastBlobName(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName);

        public abstract string GetLastBlobName(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName,
            out string etag);

        public abstract int GetBlobCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName);

        public abstract int GetBlobDirectoryCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName);

        public abstract IEnumerable<string> ListBlobs(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName);

        public abstract void DeleteDirectory(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName);

        public abstract void CopyDirectory(
            string storageAccount,
            string storageKey,
            string sourceContainerName,
            string sourceDirName,
            string targetContainerName,
            string targetDirName);
    }

    public class AzureHelper : FileHelper
    {
        private CloudBlobClient GetBlobClient(string storageAccount, string storageKey)
        {
            var credentials = new StorageCredentials(storageAccount, storageKey);

            var blobClient = (storageAccount == "devstoreaccount1")
                          ? CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient()
                          : new CloudBlobClient(new Uri(string.Format("http://{0}.blob.core.windows.net/", storageAccount)), credentials);

            return blobClient;
        }

        public override void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            Stream stream)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);
            blockBlob.UploadFromStream(stream);
        }

        public override void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            string content)
        {
            var stream = new MemoryStream(content.Length);
            using (var textWriter = new StreamWriter(stream))
            {
                textWriter.Write(content);
                textWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                UploadBlob(storageAccount, storageKey, containerName, blobName, stream);
            }
        }

        public override Stream DownloadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);
            return blockBlob.Exists() ? blockBlob.OpenRead() : null;
        }

        public override void DeleteBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);
            blockBlob.DeleteIfExists();
        }

        public override string GetLastBlobName(
        string storageAccount,
        string storageKey,
        string containerName,
        string dirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            var lastBlob = container.ListBlobs(dirName).OfType<CloudBlockBlob>()
                                                       .OrderByDescending(blob => blob.Properties.LastModified)
                                                       .Where(blob => !blob.Name.Contains("blob."))
                                                       .FirstOrDefault();
            return lastBlob != null ? lastBlob.Name.Split('/').Last() : null;
        }

        public override string GetLastBlobName(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName,
            out string etag)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            var lastBlob = container.ListBlobs(dirName).OfType<CloudBlockBlob>()
                                    .OrderBy(blob => blob.Name.Contains("$$$.$$$") ? 1 : 0)
                                    .ThenByDescending(blob => blob.Properties.LastModified)
                                    .FirstOrDefault(blob => !blob.Name.Contains("blob."));

            // No file was found
            if (lastBlob == null)
            {
                etag = null;
                return null;
            }

            etag = lastBlob.Properties.ETag;
            return lastBlob.Name.Split('/').Last();
        }

        public override int GetBlobCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            return container.ListBlobs(dirName).OfType<CloudBlockBlob>()
                                               .Count();
        }

        public override int GetBlobDirectoryCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            return container.ListBlobs(dirName).OfType<CloudBlobDirectory>()
                                               .Count();
        }

        public override IEnumerable<string> ListBlobs(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);
            return container.ListBlobs(dirName)
                            .Cast<CloudBlobDirectory>()
                            .Select(b => b.Prefix)
                            .ToList();
        }

        public override void CopyDirectory(
            string storageAccount,
            string storageKey,
            string sourceContainerName,
            string sourceDirName,
            string targetContainerName,
            string targetDirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(sourceContainerName);

            var queue = new Queue<string>();
            queue.Enqueue(sourceDirName);

            while (queue.Count > 0)
            {
                var sourceBlobPath = queue.Dequeue();

                var dir = container.GetDirectoryReference(sourceBlobPath);
                if (dir != null)
                {
                    var blobs = dir.ListBlobs().ToList();
                    foreach (var dirBlob in blobs.OfType<CloudBlobDirectory>())
                    {
                        queue.Enqueue(dirBlob.Prefix);
                    }

                    foreach (var dirBlob in blobs.OfType<CloudBlockBlob>())
                    {
                        if (Path.GetFileName(dirBlob.Name) != "desc")
                        {
                            var targetBlobName = Path.Combine(targetDirName,
                                                              dir.Prefix.Substring(sourceDirName.Length).Trim('/'),
                                                              Path.GetFileName(dirBlob.Name) ?? "");
                            using (var stream = DownloadBlob(storageAccount, storageKey, sourceContainerName, dirBlob.Name))
                            {
                                var memStream = new MemoryStream();
                                stream.CopyTo(memStream);
                                memStream.Seek(0, SeekOrigin.Begin);
                                UploadBlob(storageAccount, storageKey, targetContainerName, targetBlobName, memStream);                                    
                            }
                        }
                    }
                }
            }
        }

        public override void DeleteDirectory(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            var client = GetBlobClient(storageAccount, storageKey);
            var container = client.GetContainerReference(containerName);

            var queue = new Queue<string>();
            queue.Enqueue(dirName);

            while (queue.Count > 0)
            {
                var sourceBlobPath = queue.Dequeue();

                var dir = container.GetDirectoryReference(sourceBlobPath);
                if (dir != null)
                {
                    var blobs = dir.ListBlobs().ToList();
                    foreach (var dirBlob in blobs.OfType<CloudBlobDirectory>())
                    {
                        queue.Enqueue(dirBlob.Prefix);
                    }

                    foreach (var dirBlob in blobs.OfType<CloudBlockBlob>())
                    {
                        dirBlob.Delete();
                    }
                }
            }            
        }
    }

    public class LocalFileHelper : FileHelper
    {
        private const string TempFolder = @"C:/temp/mixed/";

        private string GetCombinedPath(bool create, params string[] pathChuncks)
        {
            var retPath = Path.Combine(pathChuncks);

            string partialPath = string.Empty;
            var pathStrings = retPath.Split('/').ToArray();
            for (int i = 1; i < pathStrings.Length; i++)
            {
                partialPath = string.Join("/",pathStrings.Take(i));
                partialPath = partialPath.Replace('.', '_');

                if (!Directory.Exists(partialPath) && create)
                {
                    Directory.CreateDirectory(partialPath);
                }
            }

            return Path.Combine(partialPath, pathStrings.Last());
        }

        private string GetCombinedDirectory(bool create, params string[] pathChuncks)
        {
            var dir = GetCombinedPath(create, pathChuncks).Replace('.', '_');
            if (!Directory.Exists(dir) && create) Directory.CreateDirectory(dir);
            return dir;
        }

        public override void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            Stream stream)
        {
            using (var fileStream = new FileStream(GetCombinedPath(true, TempFolder, containerName, blobName), FileMode.Create))
            {
                stream.CopyTo(fileStream);
            }
        }

        public override void UploadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName,
            string content)
        {
            File.WriteAllText(GetCombinedPath(true, TempFolder, containerName, blobName), content);
        }

        public override Stream DownloadBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName)
        {
            var fileName = GetCombinedPath(false, TempFolder, containerName, blobName);

            return File.Exists(fileName) ?
                new FileStream(fileName, FileMode.Open) :
                null;
        }

        public override void DeleteBlob(
            string storageAccount,
            string storageKey,
            string containerName,
            string blobName)
        {
            File.Delete(GetCombinedPath(true, TempFolder, containerName, blobName));
        }

        public override string GetLastBlobName(
        string storageAccount,
        string storageKey,
        string containerName,
        string dirName)
        {

            var lastBlob = Directory.GetFiles(GetCombinedDirectory(true, TempFolder, containerName, dirName)).Where(f => !Directory.Exists(f) && !f.Contains("blob."))
                                                                                      .OrderByDescending(File.GetLastWriteTime)
                                                                                      .FirstOrDefault();
            return lastBlob != null ? Path.GetFileName(lastBlob) : null;
        }

        public override string GetLastBlobName(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName,
            out string etag)
        {
            dirName = dirName.TrimEnd('/');
            var lastBlob = Directory.GetFiles(GetCombinedDirectory(true, TempFolder, containerName, dirName)).Where(f => !Directory.Exists(f) && !f.Contains("blob."))
                                                                                      .OrderByDescending(File.GetLastWriteTime)
                                                                                      .FirstOrDefault();
            string ret = lastBlob != null ? Path.GetFileName(lastBlob) : null;
            etag = null;
            if (lastBlob != null) etag = (ret+File.GetLastWriteTime(lastBlob).ToString(CultureInfo.InvariantCulture))
                                                                             .GetHashCode()
                                                                             .ToString(CultureInfo.InvariantCulture);
            return ret;
        }

        public override int GetBlobCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            dirName = dirName.TrimEnd('/');
            return Directory.GetFiles(GetCombinedDirectory(true, TempFolder, containerName, dirName)).Count(f => !Directory.Exists(f) && !f.Contains("blob."));
        }

        public override int GetBlobDirectoryCount(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            dirName = dirName.TrimEnd('/');
            return Directory.GetDirectories(GetCombinedDirectory(true, TempFolder, containerName, dirName)).Count();
        }

        public override IEnumerable<string> ListBlobs(
            string storageAccount,
            string storageKey,
            string containerName,
            string dirName)
        {
            throw new NotImplementedException();
        }

        public override void DeleteDirectory(string storageAccount, string storageKey, string containerName, string dirName)
        {
            var dir = GetCombinedDirectory(true, TempFolder, containerName, dirName);
            //File.Delete(dir);
            //throw new NotImplementedException();
        }

        public override void CopyDirectory(string storageAccount, string storageKey, string sourceContainerName, string sourceDirName,
            string targetContainerName, string targetDirName)
        {
            throw new NotImplementedException();
        }


    }
}
