using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.DataManipulation;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public class DataExploreQuestion : Question
    {
        private string GetTransformerElement(DataTransformer transformer)
        {
            var sb = new StringBuilder();

            var logTransformer = transformer as LogTransformer;
            if (logTransformer != null)
            {
                sb.AppendFormat("Apply log to '{0}'", logTransformer.ColumnName);
            }

            var removeRowTransformer = transformer as RemoveRowsTransformer;
            if (removeRowTransformer != null)
            {
                sb.AppendFormat("Remove rows according to filter on '{0}' (", removeRowTransformer.ColumnName);
            }
            
            sb.AppendFormat("<a href=\"javascript:window.mixedModelApp.datacontext.removeTransformer('{0}')\">Remove</a>", transformer.TransformerId);
            sb.Append(")<br>");

            return sb.ToString();
        }

        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult modelResult)
        {
            var dataTable = dataset.DataTable;
            var transformerList = dataset.DataTransformer as CompositeDataTransformer;

            if (transformerList != null && transformerList.Transformers.Any())
            {
                AddTitle("Data Transformations");
                HtmlElements.AddRange(transformerList.Transformers
                                                      .Select(GetTransformerElement));
            }

            // Get fixed effects interactions
            var charts = GetCharts(mixedModel);
            
            if (charts.Any() || dataset.ModelResult != null)
            {
                AddTitle("Data Charts");

                foreach (var chart in charts)
                {
                    AddChartElement(chart, dataTable, mixedModel);
                }
            }

            AddTitle("Variable List");
            HtmlElements.Add(
                string.Format(
                    string.Join(
                        Environment.NewLine,
                        @"<div class='dataViz'></div>
                          <table id='placeholder_raw_data'>
                              <thead>
                                          <tr>
                                                <th>Variable Id</th>
                                                <th>Name</th>
                                                <th>Value Count</th>
                                                <th>Type</th>
                                                <th>Average</th>
                                                <th>Std</th>
                                          </tr>
                                          </thead> 
                                          <tbody>
                                          </tbody>
                         </table>",
                        "<script type=\"text/javascript\">addDataTable('placeholder_raw_data');</script>")));


            return new HtmlAnswer
            {
                Question = this,
                AnswerInterpertTemplate = string.Join(Environment.NewLine, HtmlElements),
                AnswerParameters = new List<string>(),
            };
        }
    }
}
