using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using ServicesLib;
using StatisticsAnalyzerCore.StatConfig;

namespace DesktopApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ServiceContainer.EnvironmentService().IsLocal = true;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new MainForm
            {
                WindowState = FormWindowState.Maximized,
            };
            Application.Run(form);
        }
    }
}
