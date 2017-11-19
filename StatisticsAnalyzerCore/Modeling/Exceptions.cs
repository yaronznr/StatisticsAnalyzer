using System;

namespace StatisticsAnalyzerCore.Modeling
{
    class LinearModelException : Exception
    {
        public string ExceptionMessage { get; private set; }

        public LinearModelException(string message)
        {
            ExceptionMessage = message;
        }
    }

    class VariableGroupException : Exception
    {
        public string ExceptionMessage { get; private set; }

        public VariableGroupException(string message)
        {
            ExceptionMessage = message;
        }
    }

    class MixedModelException : Exception
    {
        public string ExceptionMessage { get; private set; }

        public MixedModelException(string message)
        {
            ExceptionMessage = message;
        }
    }
}
