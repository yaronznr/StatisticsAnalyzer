using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ServicesLib;
using StatisticsAnalyzerCore.Modeling;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string returnUrl)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var authTicket = new FormsAuthenticationTicket(
                    1, 
                    "Temp" + Guid.NewGuid(), 
                    DateTime.Now, 
                    DateTime.Now.AddYears(100), 
                    true, 
                    string.Empty);
                var encTicket = FormsAuthentication.Encrypt(authTicket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                Response.Cookies.Add(cookie);                
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult DownloadPage(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult Local(string returnUrl)
        {
            if (Environment.MachineName.Contains("מחשב") || Environment.MachineName.Contains("turing"))
            {
                //ServiceContainer.EnvironmentService().IsLocal = true;
                var authTicket = new FormsAuthenticationTicket(1, "Temp", DateTime.Now, DateTime.Now.AddYears(100), true, string.Empty);
                var encTicket = FormsAuthentication.Encrypt(authTicket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                Response.Cookies.Add(cookie);
                ViewBag.ReturnUrl = returnUrl;
                return View();                
            }
            throw new Exception("Local mode is only supported in desktop application");
        }

        public FileResult Download()
        {
            var fileName = ServiceContainer.StorageService().GetCurrentExcelName(User.Identity.Name);
            var dataset = ServiceContainer.ExcelDocumentService().GetExcelDocument(User.Identity.Name);
            var model = ServiceContainer.ModelService().GetModel(null, dataset, User.Identity.Name, fileName);
            var answers = ServiceContainer.AnalyzerService().GetAnswers(User.Identity.Name, model);

            var memoryStream = new MemoryStream();
            using (var fileWriter = new StreamWriter(memoryStream))
            {
                fileWriter.WriteLine("<h1>Model Summary</h1>");
                fileWriter.WriteLine(string.Join(string.Empty,
                                                 ModelAnalyzer.TranslateModel(model, dataset)
                                                              .Where(kvp => kvp.Key != "Model Intent")
                                                              .Select(kvp => string.Format("<h2>{0}</h2>", kvp.Key) + kvp.Value)));
                foreach (var answer in answers.Skip(1))
                {
                    if (answer.Question.GetFormattedQuestion() == "Script Log (raw):") continue;
                    fileWriter.WriteLine("<h1>{0}</h1>", answer.Question.GetFormattedQuestion());
                    fileWriter.WriteLine(answer.GetFormattedAnswer());
                }

                fileWriter.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.GetBuffer().Take((int)memoryStream.Length).ToArray(),
                            System.Net.Mime.MediaTypeNames.Application.Octet,
                            "analysis.doc");
            }
        }

        public FileResult DownloadRaw()
        {
            var fileName = ServiceContainer.StorageService().GetCurrentExcelName("Temp");
            var rawResultStr = ServiceContainer.StorageService().GetModelAnalysis("Temp", fileName);
            var memoryStream = new MemoryStream();
            using (var fileWriter = new StreamWriter(memoryStream))
            {
                fileWriter.Write(rawResultStr);
                fileWriter.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.GetBuffer().Take((int)memoryStream.Length).ToArray(),
                            System.Net.Mime.MediaTypeNames.Application.Octet,
                            "script.txt");
            }
        }
    }
}