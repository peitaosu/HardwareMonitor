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

            Database db = new Database("test.sqlite3");
            db.Connect();

            var options = new JsonSerializerOptions { WriteIndented = true, };

            if (result != null)
            {
                db.SaveMachine(result.MachineName, "127.0.0.1");
                foreach (var hardware in result.Hardware)
                {
                    db.SaveData(hardware.Type, hardware.Name, hardware.Identifier, JsonSerializer.Serialize(hardware.Sensors, options), 1);
                }
            }
            db.Disconnect();
            //Application.Run(new View());
            return;
        }
    }
}