using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace REngine
{
    public class InteractiveR : IDisposable
    {
        private Process _rProcess;
        private StringBuilder _errorStream;
        private object _errorLock = new object();
        public string RMessage { get; private set; }
        public delegate void NewRCommandFired(string command, string response, string error);
        public event NewRCommandFired RCommandFired;

        // Flag: Has Dispose already been called? 
        private bool _disposed;

        private string GetErrors()
        {
            string errors;
            lock (_errorLock)
            {
                errors = _errorStream.ToString();
                _errorStream = new StringBuilder();
            }
            return errors;
        }

        private string ReadToPrompt()
        {
            var sb = new StringBuilder();
            var prevChar = '\0';
            var buff = new char[1];
            _rProcess.StandardOutput.ReadBlock(buff, 0, 1);
            while (prevChar != '>' || buff[0] != ' ')
            {
                prevChar = buff[0];
                sb.Append(buff);
                _rProcess.StandardOutput.ReadBlock(buff, 0, 1);
            }
            sb.Append(buff);
            return sb.ToString().Trim(' ').Trim('>');
        }

        public InteractiveR()
        {
            _errorStream = new StringBuilder();
            var rPath = RWindowsHelper.GetRPath();
            var psi = new ProcessStartInfo
            {
                FileName = string.Format(@"{0}\R.exe", rPath),
                Arguments = "--vanilla --ess",
                WorkingDirectory = rPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            _rProcess = new Process { StartInfo = psi };
            _rProcess.ErrorDataReceived += (s, args) =>
            {
                lock (_errorLock)
                {
                    _errorStream.AppendLine(args.Data);
                }
            };
            _rProcess.Start();
            _rProcess.BeginErrorReadLine();
            RMessage = ReadToPrompt();
        }

        public string RunRCommand(string cmd, out string errors, bool showToConsole = true)
        {
            _rProcess.StandardInput.WriteLine(cmd);
            var response = ReadToPrompt();
            errors = GetErrors();
            if (RCommandFired != null && showToConsole)
            {
                RCommandFired(cmd, response, errors);
            }
            return response;
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    _rProcess.Kill();
                }
                catch (Win32Exception) // Could happen if process was closed/closing
                {
                }
            }

            // Free any unmanaged objects here. 
            //
            _disposed = true;
        }
        #endregion
    }

    public static class RHelper
    {
        public static string RunScript(string script, byte[] inputData, InteractiveR rEngine)
        {
            // Load temp path
            var tempPath = Path.GetTempPath();
            Console.WriteLine(tempPath);

            // Persist input data
            var fileName = string.Format("data.bin");
            var inputName = Path.Combine(tempPath, fileName);
            File.WriteAllBytes(inputName, inputData);
            
            // Run R script
            var resps = new List<string>();
            foreach (var scriptLine in script.Replace("{file}", inputName.Replace("\\", "/")).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string errs;
                var respWithNoError = 
                    string.Format("> {0}{1}{2}", 
                                  scriptLine,
                                  Environment.NewLine,
                                  rEngine.RunRCommand(scriptLine, out errs));
                resps.Add(string.Format("{0}{1}", respWithNoError, errs));
            }

            // Fetch output
            return string.Join("> ", resps);
        }
        public static string RunScript(string script, byte[] inputData)
        {
            // Load temp path
            var tempPath = Path.GetTempPath();
            Console.WriteLine(tempPath);
            
            // Persist input data
            var fileName = string.Format("data.bin");
            var inputName = Path.Combine(tempPath, fileName);
            File.WriteAllBytes(inputName, inputData);
            
            // Persist script
            var scriptName = Path.Combine(tempPath, string.Format("script.r"));
            File.WriteAllText(scriptName, script.Replace("{file}", inputName.Replace("\\", "/")));

            // Run R script
            var si = new ProcessStartInfo();
            si.CreateNoWindow = true;
            si.FileName = string.Format(@"{0}\bin\x64\R.exe", RWindowsHelper.GetRPath());
            si.UseShellExecute = false;
            si.Arguments = @"CMD BATCH " + scriptName;
            var process = Process.Start(si);
            if (process != null) process.WaitForExit();

            // Fetch output
            var output = File.ReadAllText(Path.Combine(tempPath, string.Format("{0}.Rout", scriptName)));
            return output;
        }
        public static int LoadRserve(int port)
        {
            // Run R script
            var si = new ProcessStartInfo();
            si.CreateNoWindow = true;
            //si.FileName = string.Format(@"{0}\bin\x64\Rserve.exe", RWindowsHelper.GetRPath());
            si.FileName = string.Format(@"{0}\Rserve.exe", RWindowsHelper.GetRPath());
            si.UseShellExecute = false;
            si.Arguments = @"--RS-port " + port;
            var proc = Process.Start(si);
            if (proc != null) return proc.Id;
            throw new Exception("Rserve.exe failed to start");
        }
        public static string RunRemoteScript(string script, byte[] inputData)
        {
            try
            {
                var scriptBase64 = HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.ASCII.GetBytes(script.ToCharArray())));
                //var request = WebRequest.Create(string.Format("http://mixedmodel.cloudapp.net:1234/?script={0}", scriptBase64));
                //var request = WebRequest.Create(string.Format("http://localhost:1234/?script={0}", scriptBase64));
                var request = WebRequest.Create(string.Format("http://ec2-52-49-188-157.eu-west-1.compute.amazonaws.com:1234/?script={0}", scriptBase64));
                request.Timeout = 600000;
                request.Method = "POST";
                var bodyStream = request.GetRequestStream();
                bodyStream.Write(inputData, 0, inputData.Length);

                var response = request.GetResponse().GetResponseStream();
                if (response == null) throw new Exception("Null response for WebRequest");
                using (var streamReader = new StreamReader(response))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }

}
