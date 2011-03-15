using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace ofp2_sync
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if (processes.Length > 1)
            {
                MessageBox.Show("VS-DLC Updater is already running. This application will now exit.");
                Application.Exit();
                Application.ExitThread();
                return;

            }
                
           

            Application.Run(new Form1());
        }
    }
}
