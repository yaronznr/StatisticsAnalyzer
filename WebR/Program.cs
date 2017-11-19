using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.IO;
using REngine;

namespace WebR
{
    public class Program : ServiceBase
    {
        HttpListener _listener = new HttpListener();
        List<InteractiveR> _engines = new List<InteractiveR>
        {
            new InteractiveR(),
            new InteractiveR(),
            new InteractiveR(),
            new InteractiveR(),
        };

        private int _nextEngine;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new Program()
            };
            Run(ServicesToRun);
        }

        private InteractiveR GetNextEngine()
        {
            var engine = _engines[_nextEngine];
            _nextEngine = (_nextEngine + 1) % _engines.Count;
            return engine;
        }

        private void GetContextCallback(IAsyncResult result)
        {
            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            var memoryStream = new MemoryStream();
            request.InputStream.CopyTo(memoryStream);
            var inputData = memoryStream.ToArray();
 
            byte[] data = Convert.FromBase64String(request.QueryString["script"]);
            var script = Encoding.ASCII.GetString(data);
            string responseString = RHelper.RunScript(script, inputData, GetNextEngine());
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var varByteStream = new MemoryStream(buffer);
            var refGZipStream = new GZipStream(varByteStream, CompressionMode.Compress, false);
            refGZipStream.BaseStream.CopyTo(response.OutputStream);
            response.AddHeader("Content-Encoding", "gzip");
            _listener.BeginGetContext(GetContextCallback, null);
        }

        protected override void OnStart(string[] args)
        {
            _listener.Prefixes.Add("http://*:1234/");
            _listener.Start();
            Console.WriteLine("Listening, hit enter to stop");
            _listener.BeginGetContext(GetContextCallback, null);
        }

        protected override void OnStop()
        {
            _listener.Stop();
        }
    }
}
