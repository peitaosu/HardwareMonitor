using System.Text.Json;

namespace HardwareMonitor
{
    internal static class Monitor
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            SettingManager.LoadFrom();

            Application.Run(new View());
            return;
        }
    }
}