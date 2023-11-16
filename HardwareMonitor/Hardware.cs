using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitor
{
    internal class Hardware
    {
        private static Hardware? _instance;
        private static readonly object _sync = new object();

        private static LibreHardwareMonitor.Hardware.Computer? _computer;

        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        public Hardware() {
            _computer = new LibreHardwareMonitor.Hardware.Computer()
            {
                IsBatteryEnabled = true,
                IsControllerEnabled = true,
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsNetworkEnabled = true,
                IsPsuEnabled = true,
                IsStorageEnabled = true,
            };
            _computer.Open();
            //_computer.Accept(new UpdateVisitor());
        }

        ~Hardware()
        {
            if (_computer != null) _computer.Close();
        }

        public static Hardware Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock (_sync)
                    {
                        if (null == _instance)
                        {
                            _instance = new Hardware();
                        }
                    }
                }
                return _instance;
            }
        }

        public dynamic GetMachine()
        {
            string ipv4 = Utils.GetLocalIPv4(NetworkInterfaceType.Ethernet);
            dynamic result = new
            {
                MachineName = Environment.MachineName,
                URI = string.IsNullOrEmpty(ipv4) ? Utils.GetLocalIPv4(NetworkInterfaceType.Wireless80211) : ipv4
            };
            return result;
        }

        public dynamic? GetHardware(string identifier = "")
        {
            bool identified = !string.IsNullOrEmpty(identifier);

            dynamic result = new
            {
                Hardware = new List<dynamic>() { }
            };

            if (_computer != null)
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    dynamic hardware_data = new
                    {
                        Type = hardware.HardwareType.ToString(),
                        Name = hardware.Name,
                        Identifier = hardware.Identifier.ToString(),
                        Sensors = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>() { },
                        SubHardwares = new List<dynamic>() { },
                    };
                    hardware.Update();
                    foreach (IHardware subhardware in hardware.SubHardware)
                    {
                        dynamic subhardware_data = new
                        {
                            Type = subhardware.HardwareType.ToString(),
                            Name = subhardware.Name,
                            Identifier = subhardware.Identifier.ToString(),
                            Sensors = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>() { },
                        };
                        subhardware.Update();

                        foreach (ISensor sensor in subhardware.Sensors)
                        {
                            string type = sensor.SensorType.ToString();
                            string id = sensor.Identifier.ToString();
                            string name = sensor.Name;
                            dynamic sensor_data = new
                            {
                                Value = sensor.Value,
                                Max = sensor.Max,
                                Min = sensor.Min,
                            };
                            if (identified && identifier.Equals(id, StringComparison.OrdinalIgnoreCase)) return sensor_data;
                            if (!subhardware_data.Sensors.ContainsKey(type)) subhardware_data.Sensors[type] = new Dictionary<string, Dictionary<string, dynamic>>();
                            if (!subhardware_data.Sensors[type].ContainsKey(id)) subhardware_data.Sensors[type][id] = new Dictionary<string, dynamic>();
                            subhardware_data.Sensors[type][id].Add(name, sensor_data);
                        }
                        if (identified && identifier.Equals(subhardware_data.Identifier, StringComparison.OrdinalIgnoreCase)) return subhardware_data;
                        hardware_data.SubHardwares.Add(subhardware_data);
                    }


                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        string type = sensor.SensorType.ToString();
                        string id = sensor.Identifier.ToString();
                        string name = sensor.Name;
                        dynamic sensor_data = new
                        {
                            Value = sensor.Value,
                            Max = sensor.Max,
                            Min = sensor.Min,
                        };
                        if (identified && identifier.Equals(id, StringComparison.OrdinalIgnoreCase)) return sensor_data;
                        if (!hardware_data.Sensors.ContainsKey(type)) hardware_data.Sensors[type] = new Dictionary<string, Dictionary<string, dynamic>>();
                        if (!hardware_data.Sensors[type].ContainsKey(id)) hardware_data.Sensors[type][id] = new Dictionary<string, dynamic>();
                        hardware_data.Sensors[type][id].Add(name, sensor_data);
                    }
                    if (identified && identifier.Equals(hardware_data.Identifier, StringComparison.OrdinalIgnoreCase)) return hardware_data;
                    result.Hardware.Add(hardware_data);
                }
            }
            if (!string.IsNullOrEmpty(identifier)) return null;
            return result;
        }
    }
}
