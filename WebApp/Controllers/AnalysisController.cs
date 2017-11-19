using ServicesLib;
using System.Web.Http;
using StatisticsAnalyzerCore.Modeling;

namespace WebApp.Controllers
{
    [Authorize]
    public class AnalysisController : ApiController
    {
        [HttpPost]
        public ModelAnalysis Data()
        {
            var formulaTask = Request.Content.ReadAsStringAsync();
            formulaTask.Wait();
            var formula = formulaTask.Result;
            return ServiceContainer.ModelService().GetModelAnalysis(User.Identity.Name, formula);
        }
    }
}
