using System.Collections.Generic;
using ServicesLib;
using System;
using System.Linq;
using System.Web.Http;
using StatisticsAnalyzerCore.DataManipulation;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class TransformerController : ApiController
    {
        private static TransformerModel ToTransformerModel(DataTransformer transformer)
        {
            if (transformer is CompositeDataTransformer) throw new Exception("No support for recursive transformers");

            var logTransformer = transformer as LogTransformer;
            if (logTransformer != null)
            {
                return new TransformerModel
                {
                    TransformerId = logTransformer.TransformerId,
                    Action = "log",
                    ColumnName = logTransformer.ColumnName,
                };
            }

            var removeRowTransformer = transformer as RemoveRowsTransformer;
            if (removeRowTransformer != null)
            {
                return new TransformerModel
                {
                    TransformerId = removeRowTransformer.TransformerId,
                    Action = "removerow",
                    ColumnName = removeRowTransformer.ColumnName,
                };
            }

            throw new Exception("Unexpected transformer type");
        }

        [HttpPost]
        public void AddTransformer()
        {
            var transformerTask = Request.Content.ReadAsStringAsync();
            transformerTask.Wait();
            var transormerString = transformerTask.Result;

            DataTransformer transformer;
            var transormerParts = transormerString.Split(',');
            switch (transormerParts[0].ToLower())
            {
                case "log":
                    transformer = new LogTransformer(transormerParts[1]);
                    break;
                case "removerow":
                    transformer = new RemoveRowsTransformer(transormerParts[1], transormerParts.Skip(2));
                    break;
                default:
                    throw new Exception("No such transformer");
            }
            ServiceContainer.ExcelDocumentService().AddDataTransformer(User.Identity.Name, transformer);
        }

        [HttpPost]
        public List<TransformerModel> GetTransformers()
        {
            var model = ServiceContainer.ExcelDocumentService().GetExcelDocument(User.Identity.Name);
            var transformerList = (CompositeDataTransformer)model.DataTransformer;

            return transformerList
                  .Transformers
                  .Select(ToTransformerModel)
                  .ToList();
        }

        [HttpDelete]
        public void RemoveTransformer()
        {
            var transformerTask = Request.Content.ReadAsStringAsync();
            transformerTask.Wait();
            var transormerId = transformerTask.Result;
            ServiceContainer.ExcelDocumentService().RemoveDataTransformer(User.Identity.Name, transormerId);
        }

    }
}
