using ServicesLib;
using StatisticsAnalyzerCore.Modeling;
using System.Net.Http;
using System.Web.Http;

namespace WebApp.Controllers
{
    [Authorize]
    public class ModelController : ApiController
    {
        private MixedModel AnalyzeModel(string formula)
        {
            return ServiceContainer.ModelService().GetModel(formula, User.Identity.Name);
        }

        [HttpGet]
        [HttpPost]
        public MixedModel Data()
        {
            string formula = null;
            if (Request.Method == HttpMethod.Post)
            {
                var formulaTask = Request.Content.ReadAsStringAsync();
                formulaTask.Wait();
                formula = formulaTask.Result;
            }

            return AnalyzeModel(formula);
        }
    }
}
