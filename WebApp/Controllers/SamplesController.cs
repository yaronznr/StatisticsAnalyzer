using System;
using System.Linq;
using ServicesLib;
using System.Web.Http;

namespace WebApp.Controllers
{
    [Authorize]
    public class SamplesController : ApiController
    {
        [HttpPost]
        public void Data()
        {
            var sampleTask = Request.Content.ReadAsStringAsync();
            sampleTask.Wait();
            var sample = sampleTask.Result;
            ServiceContainer.StorageService().CopySample(sample, User.Identity.Name);
        }

        [HttpGet]
        public SamplesGallery Samples()
        {
            return new SamplesGallery
            {
                Samples = ServiceContainer.StorageService()
                                          .GetSampleList()
                                          .Select(p => new Sample
                                                  {
                                                      Id = 1,
                                                      Name = p.Key,
                                                      Description = p.Value,
                                                  })
                                          .ToList(),
            };
        }
    }
}
