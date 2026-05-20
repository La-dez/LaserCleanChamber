using System;
using System.IO;
using System.Text.Json;

namespace LaserCleanChamber.Configuration
{
    public static class SettingsManager
    {
        private const string FilePath = "machine_config.json";

        public static AppSettings Load(bool alwaysCreateNew = false)
        {
            try
            {
                if (!File.Exists(FilePath) || alwaysCreateNew)
                {
                    var defaultConfig = new AppSettings();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                string json = File.ReadAllText(FilePath);
                var config = JsonSerializer.Deserialize<AppSettings>(json);
                return config ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(FilePath, json);
        }
    }
}