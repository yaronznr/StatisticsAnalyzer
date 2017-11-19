using System;

namespace StatisticsAnalyzerCore.Helper
{
    public static class DataConversionHelper
    {
        public static double ConvertDouble(this object cellValue)
        {
            if (cellValue is double)
            {
                return (double) cellValue;
            }

            if (cellValue is int)
            {
                return ((int) cellValue)*1.0;
            }

            throw new InvalidCastException(string.Format("Trying to cast '{0}' to double", cellValue));
        }

        public static bool IsNull(this object value)
        {
            return value == null || value == DBNull.Value;
        }
    }
}
