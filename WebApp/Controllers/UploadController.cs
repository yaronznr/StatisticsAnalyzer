using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using ExcelLib;
using ServicesLib;

namespace WebApp.Controllers
{
    public class MemoryStreamProvider : MultipartStreamProvider
    {
        public MemoryStream MemoryStream { get; set; }

        public MemoryStreamProvider()
        {
            MemoryStream = new MemoryStream();
        }

        public override Stream GetStream(HttpContent parent, System.Net.Http.Headers.HttpContentHeaders headers)
        {
            return MemoryStream;
        }
    }

    [Authorize]
    public class FileUploadController : ApiController
    {
        [HttpPost]
        public void Data()
        {
            if (Request.Content.IsMimeMultipartContent())
            {
                IEnumerable<HttpContent> parts = null;
                var streamProvider = new MemoryStreamProvider();
                Task.Factory
                    .StartNew(() => parts = Request.Content.ReadAsMultipartAsync(streamProvider).Result.Contents,
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning, // guarantees separate thread
                        TaskScheduler.Default)
                    .ContinueWith(t =>
                        {
                            if (t.IsFaulted || t.IsCanceled)
                                throw new HttpResponseException(HttpStatusCode.InternalServerError);

                            streamProvider.MemoryStream.Seek(0, SeekOrigin.Begin);
                            ServiceContainer.StorageService().UploadExcel(
                                User.Identity.Name, 
                                parts.First().Headers.ContentDisposition.FileName.Trim('"'),
                                streamProvider.MemoryStream);
                        })
                    .Wait();
            }
            else
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted"));                
            }
        }

        [HttpGet]
        public List<string> Sheets()
        {
            var stream = ServiceContainer.StorageService().GetCurrentExcelFile(User.Identity.Name);
            return ExcelDocument.GetSheetsList(stream);
        }

        [HttpPost]
        public void Sheet()
        {
            var sheetNameTask = Request.Content.ReadAsStringAsync();
            sheetNameTask.Wait();
            var sheetName = sheetNameTask.Result;

            var user = User.Identity.Name;
            ServiceContainer.StorageService().SetSheetName(
                user, 
                ServiceContainer.StorageService().GetCurrentExcelName(user),
                sheetName);
        }
    }
}
