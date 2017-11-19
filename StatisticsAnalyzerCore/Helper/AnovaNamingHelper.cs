using System.Collections.Generic;
using System.Linq;
using StatisticsAnalyzerCore.DataExplore;

namespace StatisticsAnalyzerCore.Helper
{
    public static class AnovaNamingHelper
    {
        public static string GetAnovaName(ModelDataset dataset, ICollection<string> variableGroup, bool addSpace)
        {
            if (variableGroup.Count <= 1)
            {
                return string.Empty;
            }

            string retValue = variableGroup.All(v => dataset.DataTable.Columns[v].DataType == typeof(string)) ? "ANOVA" : "ANCOVA";
            return retValue + (addSpace ? " " : "");
        }
    }
}
