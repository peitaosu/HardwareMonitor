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

            dynamic? result = Hardware.Instance.GetData();

            SettingManager.LoadFrom();

            Database db = new Database(SettingManager.GetSetting().DatabasePath);
            db.Connect();

            if (result != null)
            {
                var machine = db.GetMachine(result.MachineName, result.URI);
                if (machine == null)
                    db.SaveMachine(result.MachineName, result.URI);
                    machine = db.GetMachine(result.MachineName, result.URI);
                foreach (var hardware in result.Hardware)
                {
                    db.SaveData(hardware.Type, hardware.Name, hardware.Identifier, JsonSerializer.Serialize(hardware.Sensors, SettingManager.GetSetting().JsonOptions), machine);
                }
            }
            db.Disconnect();
            Application.Run(new View());
            return;
        }
    }
}