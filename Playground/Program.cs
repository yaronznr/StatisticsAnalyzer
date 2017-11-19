using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using REngine;
using RserveCli;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var r = new InteractiveR())
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(r.RMessage);
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("> ");
                    var cmd = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.White;
                    string errs;
                    Console.Write(r.RunRCommand(cmd, out errs));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(errs);
                }
            }
        }
    }
}
