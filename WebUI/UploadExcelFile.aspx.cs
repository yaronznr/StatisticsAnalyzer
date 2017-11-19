using AzureCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebUI
{
    public partial class UploadExcelFile : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Guid userId = UserManager.GetUserId(Request, Response);

            FileDiv.InnerText = AzureHelper.GetLastBlobName(
                    AzureHelper.DefaultStorageAccount,
                    AzureHelper.DefaultStorageKey,
                    "files",
                    string.Format("{0}/", userId.ToString())) ?? "No file chosen |";

            if (File1.PostedFile != null && File1.PostedFile.ContentLength > 0)
            {
                AzureHelper.UploadBlob(
                    AzureHelper.DefaultStorageAccount,
                    AzureHelper.DefaultStorageKey,
                    "files",
                    string.Format("{0}/{1}", userId.ToString(), Path.GetFileName(File1.PostedFile.FileName)),
                    File1.PostedFile.InputStream);

                FileDiv.InnerText = File1.PostedFile.FileName;
                File1.PostedFile.InputStream.Seek(0, SeekOrigin.Begin);
                AzureHelper.UploadBlob(
                    AzureHelper.DefaultStorageAccount,
                    AzureHelper.DefaultStorageKey,
                    "files",
                    string.Format("{0}/blob.xlsx", userId.ToString()),
                    File1.PostedFile.InputStream);
            }
        }
    }
}