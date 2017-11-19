using System;

namespace StatisticsAnalyzerCore.Helper
{
    public static class DistributionHelper
    {
        public static double ComputeZ(double value)
        {
            // constants
            var a1 = 0.254829592;
            var a2 = -0.284496736;
            var a3 = 1.421413741;
            var a4 = -1.453152027;
            var a5 = 1.061405429;
            var p = 0.3275911;
            
            // Save the sign of x
            var sign = 1;
            if (value < 0)
            {
                sign = -1;
            }
            value = Math.Abs(value)/Math.Sqrt(2.0);
 
            // A&S formula 7.1.26
            var t = 1.0/(1.0 + p*value);
            var y = 1.0 - (((((a5*t + a4)*t) + a3)*t + a2)*t + a1)*t*Math.Exp(-value*value);

            return 0.5*(1.0 + sign*y);
        }

        public static double ComputeInverseZ(double z)
        {
            var value = -10.0;

            var step = 1.0;
            for (var i = 0; i < 6; i++)
            {
                while (ComputeZ(value + step) < z && value < 10.0)
                    value += step;

                step = step/10.0;
            }

            return value;
        }

    }
}
