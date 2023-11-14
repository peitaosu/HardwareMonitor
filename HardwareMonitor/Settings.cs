using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace HardwareMonitor
{
    internal class Settings
    {
        private static Settings? _instance;
        private static readonly object _sync = new object();
        private static string default_setting = "settings.json";
        private static dynamic? _settings = null;

        public Settings(string? setting_path = null)
        {
            if (string.IsNullOrEmpty(setting_path)) setting_path = default_setting;
            if (!File.Exists(setting_path)) throw new FileNotFoundException(setting_path);
            _settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(setting_path));
        }

        public static Settings Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock (_sync)
                    {
                        if (null == _instance)
                        {
                            _instance = new Settings();
                        }
                    }
                }
                return _instance;
            }
        }

        public object Get(string setting)
        {
            if (_settings == null)
            {
                _instance = new Settings();
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return _settings[setting];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public void Set(string setting, object value)
        {
            if (_settings == null)
            {
                _instance = new Settings();
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            _settings[setting] = value;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public bool Save(string? setting_path = null)
        {
            // TODO
            return true;
        }
    }
}
