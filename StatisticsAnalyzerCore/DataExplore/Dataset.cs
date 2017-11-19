using System.Data;
using StatisticsAnalyzerCore.DataManipulation;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.DataExplore
{
    public class ModelDataset
    {
        public DataTable DataTable { get; set; }
        public TableStats TableStats { get; set; }
        public DataTransformer DataTransformer { get; set; }
        public MixedModelResult ModelResult { get; set; }
    }
}
