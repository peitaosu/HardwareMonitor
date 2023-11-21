using System.Text.Json;
using System.Timers;

namespace HardwareMonitor
{
    internal class Monitor
    {
        private static Monitor _instance;
        private static readonly object _sync = new object();
        private static bool _running = false;
        private static Database _database;
        private static long _machine_id = 0;
        private static int _frequency = 5;
        private static System.Timers.Timer _timer;

        public static Monitor Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock (_sync)
                    {
                        if (null == _instance)
                        {
                            _instance = new Monitor();
                        }
                    }
                }
                return _instance;
            }
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void Run()
        {
            if (!_running) return;
            dynamic result = Hardware.Instance.GetHardware();
            long timestamp = Utils.GetUTCTimestamp();

            if (result != null)
            {
                List<Tuple<string, string, string, string, string, long, long>> records = new List<Tuple<string, string, string, string, string, long, long>>();
                    
                foreach (var hardware in result.Hardware)
                {
                    records.Add(new Tuple<string, string, string, string, string, long, long>(
                        hardware.Type, hardware.Name, hardware.Identifier, JsonSerializer.Serialize(hardware.Sensors), "", _machine_id, timestamp
                    ));
                    foreach (var subhardware in hardware.SubHardwares)
                    {
                        records.Add(new Tuple<string, string, string, string, string, long, long>(
                            subhardware.Type, subhardware.Name, subhardware.Identifier, JsonSerializer.Serialize(subhardware.Sensors), hardware.Identifier, _machine_id, timestamp
                        ));
                    }
                }
                _database.SaveHardware(records);
            }
        }

        public void Start()
        {
            _running = true;
            _timer = new System.Timers.Timer(_frequency * 1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void Stop()
        {
            _running = false;
        }

        public void MonitorHardware(Database db, long machine_id, int frequency)
        {
            _database = db;
            _machine_id = machine_id;
            _frequency = frequency;
        }

        public List<dynamic> FetchHardware(long machine_id, int last)
        {
            var result = _database.FetchData(machine_id, last);
            return result;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                Run();
            });
        }


    }
}