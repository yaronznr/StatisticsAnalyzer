using System;
using System.Collections.Generic;
using StatisticsAnalyzerCore.Modeling;

namespace StatisticsAnalyzerCore.Helper
{
    public static class StatisticsTextHelper
    {
        public static string CreatePValueReport(double pValue)
        {
            return CreatePValueReport(null, 0, pValue);
        }

        public static string CreatePValueReport(string statistic, double statisticValue, double pValue)
        {
            var reportComponents = new List<string>();

            if (!string.IsNullOrEmpty(statistic))
            {
                reportComponents.Add(string.Format("{0}={1:F3}", statistic, statisticValue));
            }

            string pValueReport;
            if (pValue < 0.00001)
            {
                pValueReport = "P<0.00001";
            }
            else
            {
                pValueReport = string.Format("P={0:F5}", pValue);
            }
            reportComponents.Add(pValueReport);

            return string.Format("({0})", string.Join(", ", reportComponents));

        }

        public static string CreateLargerThanStatement(string responseVariable,
                                                       string variable1,
                                                       string variable2,
                                                       double estimate)
        {
            if (estimate > 0 && responseVariable == variable1 ||
                estimate < 0 && responseVariable == variable2)
            {
                return string.Format("{0} is greater than {1} by {2}. ", variable1, variable2, Math.Abs(estimate));
            }

            return string.Format("{0} is greater than {1} by {2}. ", variable2, variable1, Math.Abs(estimate));
        }

        public static string CreateBinomialLargerThanStatement(
            string predictedVariable,
            string predictedZeroLevel,
            string responseVariableName,
            string responseVariable,
            string variable1,
            string variable2,
            double estimate)
        {
            var side1 = variable2;
            var side2 = variable1;

            if (estimate > 0 && responseVariable == variable1 ||
                estimate < 0 && responseVariable == variable2)
            {
                side1 = variable1;
                side2 = variable2;
            }

            return string.Format("{3} odds of not being {4} are bigger by {2:F3} when {5} has value of {0} (and not {1}). ",
                variable1,
                variable2,
                Math.Exp(Math.Abs(estimate)),
                predictedVariable,
                predictedZeroLevel,
                responseVariableName);
        }

        public static string CreateBinomialSlopeStatement(
            string predictedVariable,
            string zeroLevelValue,
            string covariate,
            double slope)
        {
            return CreateBinomialSlopeStatement(predictedVariable, zeroLevelValue, covariate, slope, true);
        }

        public static string CreateBinomialSlopeStatement(
            string predictedVariable,
            string zeroLevelValue,
            string covariate,
            double slope,
            bool hasOtherVariables)
        {
            bool isNegative = slope < 0;
            double absSlope = Math.Abs(slope);

            return string.Format("Estimated slope is {0}. This means that, in the given model, ", slope) +
                   string.Format("{0}", hasOtherVariables ? "when keeping other variables fixed, " : "") +
                   string.Format("we expect {0} odds of not being {1} to double with every {4} of {2} in {3}. ",
                        predictedVariable,
                        zeroLevelValue,
                        Math.Log(2, Math.Exp(1)) / Math.Abs(slope),
                        covariate,
                        isNegative ? "decrease" : "increase");
        }

        public static string CreateSlopeStatement(string predictedVariable,
                                                  string covariate,
                                                  double slope)
        {
            return CreateSlopeStatement(predictedVariable, covariate, slope, true);
        }

        public static string CreateSlopeStatement(string predictedVariable,
                                                  string covariate,
                                                  double slope,
                                                  bool hasOtherVariables)
        {
            bool isNegative = slope < 0;
            double absSlope = Math.Abs(slope);

            return string.Format("Estimated slope is {0}. This means that, in the given model, ", slope) +
                   string.Format("{0}", hasOtherVariables ? "when keeping other variables fixed, " : "") +
                   string.Format("we expect a unit increase in {0} ", covariate) +
                   string.Format("to create a {0} of {1} in {2}. ", isNegative ? "decrease" : "increase", absSlope, predictedVariable);
        }

        public static string CreateInteractionSlopeStatement(string predictedVariable, 
                                                             string categoryVariable,
                                                             string category1,
                                                             string category2,
                                                             string continousVariable,
                                                             double baseSlope,
                                                             double slopeDiff,
                                                             SingleLevelEfectResult interactionEffect)
        {
            return string.Format("This means that for different values of {0} the effect of {1} on {6} is different, " +
                                 "when {0} is {2} the slope is {4} and when {0} is {3} the slope is {5}. " +
                                 "We have a difference of {7} in slopes {8}. We expect that with each unit increase in {1} " +
                                 "the value of {6} we {10} by {9} more with {3} than with {4}",
                                 categoryVariable,
                                 continousVariable,
                                 category1,
                                 category2,
                                 baseSlope,
                                 baseSlope + slopeDiff,
                                 predictedVariable,
                                 slopeDiff,
                                 CreatePValueReport("T", interactionEffect.TValue, interactionEffect.PValue),
                                 Math.Abs(slopeDiff),
                                 slopeDiff > 0 ? "increase" : "decrease");

        }
    }
}
