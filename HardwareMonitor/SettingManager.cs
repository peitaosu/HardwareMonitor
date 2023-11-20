using System;
using System.Text.Json;
using System.Reflection;

namespace HardwareMonitor
{
    class Setting
    {
        public string DatabasePath { get; set; }
        public string LogPath { get; set; }

        public string WebCachePath { get; set; }

        public bool UseOnlinePage { get; set; }

        public JsonSerializerOptions JsonOptions { get; set; }
    }

    class SettingManager
    {
        private static string default_setting = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
        private static string custom_setting = Path.Combine(Path.GetTempPath(), "settings.json");
        private static Setting? _settings = null;

        public static Setting GetSetting() {
            return _settings;
        }

        public static void LoadFrom(string? setting_path = null)
        {
            if (!string.IsNullOrEmpty(setting_path) && File.Exists(setting_path))
            {
                _settings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(setting_path));
            }
            else
            {
                if (File.Exists(custom_setting))
                {
                    _settings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(custom_setting));
                }
                else
                {
                    _settings = JsonSerializer.Deserialize<Setting>(File.ReadAllText(default_setting));
                }
            }
            Setting defaults = JsonSerializer.Deserialize<Setting>(File.ReadAllText(default_setting));

            var properties = typeof(Setting).GetProperties().Where(prop => prop.CanRead && prop.CanWrite);
            foreach (var prop in properties)
            {
                if (prop.GetValue(_settings, null) == null)
                    prop.SetValue(_settings, prop.GetValue(defaults, null), null);
            }
        }

        public static bool SaveTo(string setting_path, IDictionary<string, object>? specific = null)
        {
            if (specific != null)
            {
                foreach (var item in specific)
                {
                    var properties = typeof(Setting).GetProperties().Where(prop => prop.CanRead && prop.CanWrite);
                    foreach (var prop in properties)
                    {
                        if (prop.Name == item.Key) prop.SetValue(_settings, item.Value, null);
                    }
                }
            }
            if (!string.IsNullOrEmpty(setting_path))
            {
                File.WriteAllText(setting_path, JsonSerializer.Serialize(_settings, _settings.JsonOptions));
            }
            else
            {
                File.WriteAllText(custom_setting, JsonSerializer.Serialize(_settings, _settings.JsonOptions));
            }
            return true;
        }

    }
}
