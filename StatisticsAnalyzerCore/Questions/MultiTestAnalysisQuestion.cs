using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public class MultiTestAnalysisQuestion : Question
    {
        private List<string> _htmlElements = new List<string>();

        private void AddTitle(string title)
        {
            _htmlElements.Add(string.Format("<h2>{0}</h2>", title));
        }

        private string CreateTable(List<string> header,
                                   List<List<string>> tableData,
                                   string tableId,
                                   bool addTableInteraction)
        {
            var tb = new StringBuilder();

            tb.AppendLine(string.Format("<table id={0}>", tableId));
            tb.AppendFormat("<thead><tr><th>{0}</th></tr></thead>", string.Join("</th><th>", header));
            tb.AppendLine("<tbody>");
            foreach (var tableRow in tableData)
            {
                tb.AppendFormat("<tr><td>{0}</td></tr>", string.Join("</td><td>", tableRow));
            }
            tb.AppendLine("</tbody>");
            tb.AppendLine("</table>");
            if (addTableInteraction)
            {
                tb.AppendLine(string.Format("<script type=\"text/javascript\">addFixedTable('{0}');</script>", tableId));
            }

            return tb.ToString();
        }

        public Answer AnalyzeAnswer(Dictionary<string, VariableEffect> variableEffects)
        {
            AddTitle("Variable Analyses");

            _htmlElements.Add(
                CreateTable(new List<string> { "Variable Name", "Max Effect", "F", "DF", "P.Value" },
                            variableEffects.Select(
                            v =>
                            new List<string>
                            {
                                v.Key.ToString(CultureInfo.InvariantCulture),
                                v.Value.MaxEffect.ToString(CultureInfo.InvariantCulture),
                                v.Value.F.ToString(CultureInfo.InvariantCulture),
                                v.Value.Df.ToString(CultureInfo.InvariantCulture),
                                v.Value.PValue.ToString(CultureInfo.InvariantCulture),
                            }).ToList(),
                            "placeholder_multianalysis",
                            true));

            return new HtmlAnswer
            {
                Question = this,
                AnswerInterpertTemplate = string.Join(Environment.NewLine, _htmlElements),
                AnswerParameters = new List<string>(),
            };
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult modelResult)
        {
            throw new NotImplementedException();
        }
    }
}
