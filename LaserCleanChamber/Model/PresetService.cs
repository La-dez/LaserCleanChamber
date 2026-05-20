using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model
{
    public class PresetService
    {
        private const string FileName = "Presets.json";

        public static void Save(IEnumerable<LaserPreset> presets)
        {
            var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
        }

        public static List<LaserPreset> Load()
        {
            if (!File.Exists(FileName))
                return LoadDefault();

            var json = File.ReadAllText(FileName);
            var list = JsonSerializer.Deserialize<List<LaserPreset>>(json);
            if(list == null)
                return LoadDefault();
            return new List<LaserPreset>(list);
        }

        public static List<LaserPreset> LoadDefault()
        {
            return new List<LaserPreset>() { GetDefaultPreset("Режим 1") };
        }

        public static LaserPreset GetDefaultPreset(string name)
        {
            return new LaserPreset()
            {
                Name = name,
                Power = 10,
                ScanWidth = 20,
                ScanSpeed = 30
            };
        }

    }
}
