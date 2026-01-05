using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cool1Windows.Models;

namespace Cool1Windows.Services
{
    public class ConfigService
    {
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cool1Windows",
            "history.json"
        );

        private static readonly string WindowSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cool1Windows",
            "window.json"
        );

        public static List<AppInfo> LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var json = File.ReadAllText(HistoryPath);
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
                var dir = Path.GetDirectoryName(HistoryPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(HistoryPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save history failed: {ex.Message}");
            }
        }

        public static WindowSettings LoadWindowSettings()
        {
            try
            {
                if (File.Exists(WindowSettingsPath))
                {
                    var json = File.ReadAllText(WindowSettingsPath);
                    return JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load window settings failed: {ex.Message}");
            }
            return new WindowSettings();
        }

        public static void SaveWindowSettings(WindowSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(WindowSettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(WindowSettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save window settings failed: {ex.Message}");
            }
        }
    }
}
