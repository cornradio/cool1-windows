using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cool1Windows.Models;

namespace Cool1Windows.Services
{
    public class AppScanner
    {
        public static List<AppInfo> ScanInstalledApps()
        {
            var apps = new List<AppInfo>();
            
            // 扫描开始菜单目录
            string[] searchPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs)
            };

            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    ScanDirectory(path, apps);
                }
            }

            return apps.GroupBy(a => a.Path).Select(g => g.First()).OrderBy(a => a.Name).ToList();
        }

        private static void ScanDirectory(string dir, List<AppInfo> apps)
        {
            try
            {
                // 获取快捷方式
                foreach (var file in Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories))
                {
                    apps.Add(new AppInfo
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning {dir}: {ex.Message}");
            }
        }

        public static List<AppInfo> GetRunningApps()
        {
            var runningApps = new List<AppInfo>();
            var processes = System.Diagnostics.Process.GetProcesses();
            
            foreach (var proc in processes)
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainModule?.FileName))
                    {
                        var path = proc.MainModule.FileName;
                        runningApps.Add(new AppInfo
                        {
                            Name = proc.ProcessName,
                            Path = path
                        });
                    }
                }
                catch
                {
                }
            }

            return runningApps.GroupBy(a => a.Path).Select(g => g.First()).OrderBy(a => a.Name).ToList();
        }
    }
}
