using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace REngine
{
    public static class RWindowsHelper
    {
        public static string GetRPathBase()
        {
            string retValue = null;
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var openSubKey = hklm.OpenSubKey(@"Software\R-core\R\"))
                    {
                        if (openSubKey != null)
                        {
                            var keyNames = openSubKey.GetSubKeyNames();
                            var latestVersion = keyNames.OrderByDescending(e => e).First();
                            var value = Registry.GetValue(string.Format(@"HKEY_LOCAL_MACHINE\Software\R-core\R\{0}\", latestVersion), "InstallPath", null);
                            if (value != null) retValue = value.ToString();
                        }
                    }
                }

                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var openSubKey = hklm.OpenSubKey(@"Software\R-core\R\"))
                    {
                        if (openSubKey != null)
                        {
                            var keyNames = openSubKey.GetSubKeyNames();
                            var latestVersion = keyNames.OrderByDescending(e => e).First();
                            var value = Registry.GetValue(string.Format(@"HKEY_LOCAL_MACHINE\Software\R-core\R\{0}\", latestVersion), "InstallPath", null);
                            if (value != null)
                            {
                                retValue = value.ToString();                                
                            }                            
                        }
                        
                    }                    
                }
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }

            if (string.IsNullOrEmpty(retValue))
            {
                retValue = Path.Combine(Environment.CurrentDirectory, "RBin");
            }

            return retValue;
        }

        public static string GetRPath()
        {
            return Path.Combine(GetRPathBase(), @"bin\x64");
        }
    }
}
