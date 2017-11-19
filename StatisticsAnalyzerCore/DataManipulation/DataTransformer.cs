using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Serialization;

namespace StatisticsAnalyzerCore.DataManipulation
{
    [Serializable]
    [XmlInclude(typeof(LogTransformer))]
    [XmlInclude(typeof(RemoveRowsTransformer))]
    [XmlInclude(typeof(CompositeDataTransformer))]
    public abstract class DataTransformer
    {
        [XmlElement]
        public string TransformerId { get; set; }

        protected DataTransformer()
        {
            TransformerId = Guid.NewGuid().ToString();
        }

        public abstract void TransformDataTable(DataTable dataTable);
    }

    [Serializable]
    public class CompositeDataTransformer : DataTransformer
    {
        [XmlArray]
        public List<DataTransformer> Transformers { get; set; }

        public CompositeDataTransformer() {}
        public CompositeDataTransformer(List<DataTransformer> transformers)
        {
            Transformers = transformers;
        }

        public override void TransformDataTable(DataTable dataTable)
        {
            foreach (var dataTransformer in Transformers)
            {
                dataTransformer.TransformDataTable(dataTable);
            }
        }
    }
}
