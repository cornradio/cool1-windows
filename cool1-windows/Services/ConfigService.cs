using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cool1Windows.Models;

namespace Cool1Windows.Services
{
    public class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cool1Windows",
            "history.json"
        );

        public static List<AppInfo> LoadHistory()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<List<AppInfo>>(json) ?? new List<AppInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load history failed: {ex.Message}");
            }
            return new List<AppInfo>();
        }

        public static void SaveHistory(List<AppInfo> history)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save history failed: {ex.Message}");
            }
        }
    }
}
