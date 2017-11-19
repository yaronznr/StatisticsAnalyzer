using System;
using System.Collections.Generic;
using StatisticsAnalyzerCore.DataExplore;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Questions
{
    public class RawDataViewerQuestion : Question
    {
        public override Answer AnalyzeAnswer(ModelDataset dataset, MixedLinearModel mixedModel, MixedModelResult modelResult)
        {
            var htmlElements = new List<string>();

            htmlElements.Add(
                string.Format(
                    string.Join(
                        Environment.NewLine,
                        "<table id=\"placeholder_raw_data\" style=\"width:600px;height:300px\">" +
                        @"    <thead>
                              <tr>
                                    <th>Variable Id</th>
                                    <th>Name</th>
                                    <th>Value Count</th>
                                    <th>Type</th>
                              </tr>
                              </thead> 
                              <tbody>
                              </tbody>" +
                        "</table>",
                        "<script type=\"text/javascript\">addDataTable('placeholder_raw_data');</script>")));

            return new HtmlAnswer
            {
                Question = this,
                AnswerInterpertTemplate = string.Join(Environment.NewLine, htmlElements),
                AnswerParameters = new List<string>(),
            };
        }
    }
}
