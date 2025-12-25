using System;
using System.Windows;
using Microsoft.Win32;

namespace Cool1Windows.Services
{
    public class ThemeService
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public void Initialize()
        {
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    ApplySystemTheme();
                }
            };
            ApplySystemTheme();
        }

        public void ApplyTheme(string themeName)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            try
            {
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/Themes/{themeName}Theme.xaml", UriKind.Absolute)
                };

                var styles = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Themes/Styles.xaml", UriKind.Absolute)
                };

                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(newTheme);
                app.Resources.MergedDictionaries.Add(styles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        public void ApplySystemTheme()
        {
            bool isLightTheme = true;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key?.GetValue(RegistryValueName) is int value)
                    {
                        isLightTheme = value != 0;
                    }
                }
            }
            catch { }

            ApplyTheme(isLightTheme ? "Light" : "Dark");
        }
    }
}
