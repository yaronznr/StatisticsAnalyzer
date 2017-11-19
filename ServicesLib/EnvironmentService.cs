using System;

namespace ServicesLib
{
    public class EnvironmentService
    {
        public bool IsLocal { get; set; }

        /*public bool IsLocalBuild()
        {
            return (Environment.MachineName.Contains("מחשב"));
        }*/
    }
}
