using System.Collections.Generic;

namespace StatisticsAnalyzerCore.DataExplore
{
    public enum ColumnClassification
    {
        UniqueId,
        SingleValue,
        Measure,
        Regressor,
        Grouping,
    }

    public enum ColumnBalanace
    {
        Balanced,
        SemiBalanced,
        NonBalanaced,
    }

    public enum LinkAttributes
    {
        None,
        Nested,
        StrictCovering,
        StatisticCovering,
    }

    public enum ColumnRepeated
    {
        NonRepeated,
        StrictRepeated,
        StatisticRepeated,
    }

    public class ColumnRelation
    {
        public HashSet<LinkAttributes> RelationAttributes { get; private set; }

        public ColumnRelation()
        {
            RelationAttributes = new HashSet<LinkAttributes>();
        }
    }

    public class TableAnalysis
    {
        public Dictionary<string, ColumnClassification> ColumnClassifications { get; private set; }
        public Dictionary<string, ColumnBalanace> ColumnBalanaces { get; private set; }
        public Dictionary<string, HashSet<string>> CrossBalanaceGroup { get; private set; }
        public Dictionary<string, ColumnRepeated> ColumnRepeated { get; private set; }
        public Dictionary<string, Dictionary<string, ColumnRelation>> ColumnGraph { get; private set; }
        public bool IsGraphComputed { get; set; } // Graph is not computed when we have more than 1000 columns

        public TableAnalysis(Dictionary<string, ColumnClassification> columnClassifications,
                             Dictionary<string, ColumnBalanace> columnBalanaces,
                             Dictionary<string, HashSet<string>> crossBalanaceGroup,
                             Dictionary<string, ColumnRepeated> columnRepeated,
                             Dictionary<string, Dictionary<string, ColumnRelation>> columnGraph,
                             bool isGraphComputed)
        {
            ColumnClassifications = columnClassifications;
            ColumnBalanaces = columnBalanaces;
            CrossBalanaceGroup = crossBalanaceGroup;
            ColumnRepeated = columnRepeated;
            ColumnGraph = columnGraph;
            IsGraphComputed = isGraphComputed;
        }
    }
}
