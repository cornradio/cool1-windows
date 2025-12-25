using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Cool1Windows.Models;
using Cool1Windows.Services;
using Microsoft.Win32;

namespace Cool1Windows.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private ObservableCollection<AppInfo> _apps = new();
        private ObservableCollection<AppInfo> _history = new();
        private AppInfo? _selectedApp;
        private bool _showOnlyFavorites;
        private string _sortMode = "最近启动"; // 默认排序模式

        public ObservableCollection<AppInfo> Apps
        {
            get => _apps;
            set => SetProperty(ref _apps, value);
        }

        public ObservableCollection<AppInfo> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        public AppInfo? SelectedApp
        {
            get => _selectedApp;
            set => SetProperty(ref _selectedApp, value);
        }

        public bool ShowOnlyFavorites
        {
            get => _showOnlyFavorites;
            set
            {
                if (SetProperty(ref _showOnlyFavorites, value)) RefreshDisplayedHistory();
            }
        }

        public string SortMode
        {
            get => _sortMode;
            set
            {
                if (SetProperty(ref _sortMode, value)) RefreshDisplayedHistory();
            }
        }

        public ObservableCollection<AppInfo> DisplayedHistory { get; } = new();

        public ICommand LaunchAppCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand SelectManualCommand { get; }
        public ICommand LaunchSelectedCommand { get; }

        public MainViewModel()
        {
            LaunchAppCommand = new RelayCommand(p => { if (p is AppInfo a) LaunchApp(a); });
            ToggleFavoriteCommand = new RelayCommand(p => { if (p is AppInfo a) ToggleFavorite(a); });
            DeleteCommand = new RelayCommand(p => { if (p is AppInfo a) DeleteFromHistory(a); });
            KillCommand = new RelayCommand(p => { if (p is AppInfo a) KillApp(a); });
            SelectManualCommand = new RelayCommand(_ => SelectAppManually());
            LaunchSelectedCommand = new RelayCommand(_ => { if (SelectedApp != null) LaunchApp(SelectedApp); }, _ => SelectedApp != null);

            LoadData();

            Console.WriteLine("[Main] Initializing Running Status Timer...");
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) => RefreshRunningStatus();
            timer.Start();
        }

        private void RefreshRunningStatus()
        {
            try
            {
                var processes = Process.GetProcesses();
                var processNames = processes.Select(p => p.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                
                // 方案2所需的路径集合 (由于耗时，我们可以只拿一次)
                // var processPaths = processes.Select(p => { try { return p.MainModule?.FileName; } catch { return null; } }).Where(x => x!=null).ToHashSet();

                foreach (var app in History)
                {
                    bool isRunning = false;
                    if (!string.IsNullOrEmpty(app.RealProcessName))
                        isRunning = processNames.Contains(app.RealProcessName);
                    
                    if (!isRunning)
                    {
                        var exeName = System.IO.Path.GetFileNameWithoutExtension(app.Path);
                        isRunning = processNames.Contains(exeName);
                    }

                    if (app.IsRunning != isRunning)
                    {
                        app.IsRunning = isRunning;
                        Console.WriteLine($"[Main] Status Changed: {app.Name} -> {(isRunning ? "Running" : "Stopped")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] Refresh Loop Error: {ex.Message}");
            }
        }

        public void LoadData()
        {
            Apps = new ObservableCollection<AppInfo>(AppScanner.ScanInstalledApps());
            History = new ObservableCollection<AppInfo>(ConfigService.LoadHistory());
            foreach (var app in History)
            {
                if (string.IsNullOrEmpty(app.RealProcessName))
                {
                    string target = ShortcutService.ResolveShortcut(app.Path);
                    app.RealProcessName = System.IO.Path.GetFileNameWithoutExtension(target);
                }
            }
            RefreshDisplayedHistory();
        }

        public void RefreshDisplayedHistory()
        {
            var query = History.AsEnumerable();

            if (ShowOnlyFavorites)
            {
                query = query.Where(a => a.IsFavorite);
            }

            if (SortMode == "最近启动")
            {
                query = query.OrderByDescending(a => a.LastLaunched ?? DateTime.MinValue).ThenBy(a => a.Name);
            }
            else
            {
                query = query.OrderBy(a => a.Name);
            }

            DisplayedHistory.Clear();
            foreach (var app in query)
            {
                DisplayedHistory.Add(app);
            }
        }

        public void LaunchApp(AppInfo app)
        {
            Console.WriteLine($"[Main] Launching: {app.Name} ({app.Path})");
            try
            {
                // 解析快捷方式
                string realPath = ShortcutService.ResolveShortcut(app.Path);
                if (string.IsNullOrEmpty(app.RealProcessName))
                {
                    app.RealProcessName = System.IO.Path.GetFileNameWithoutExtension(realPath);
                    Console.WriteLine($"[Main] Resolved Real Process Name: {app.RealProcessName}");
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = app.Path,
                    UseShellExecute = true
                };
                var proc = Process.Start(psi);
                
                // 如果是直接启动 exe，可以记录进程名
                if (proc != null)
                {
                     try { app.RealProcessName = proc.ProcessName; } catch { }
                }

                UpdateHistory(app);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] Launch Failed: {ex.Message}");
                System.Windows.MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        private void UpdateHistory(AppInfo app)
        {
            var existing = History.FirstOrDefault(a => a.Path == app.Path);
            if (existing != null)
            {
                existing.LastLaunched = DateTime.Now;
            }
            else
            {
                app.LastLaunched = DateTime.Now;
                History.Insert(0, app);
            }
            ConfigService.SaveHistory(History.ToList());
            RefreshDisplayedHistory();
        }

        public void SelectAppManually()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "应用程序 (*.exe;*.lnk)|*.exe;*.lnk|所有文件 (*.*)|*.*",
                Title = "手动选择应用程序"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var app = new AppInfo
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName),
                    Path = openFileDialog.FileName
                };
                
                if (!Apps.Contains(app))
                {
                    Apps.Add(app);
                    var sortedList = Apps.OrderBy(x => x.Name).ToList();
                    Apps.Clear();
                    foreach(var item in sortedList) Apps.Add(item);
                }
                SelectedApp = Apps.FirstOrDefault(a => a.Path == app.Path) ?? app;
            }
        }

        public void ToggleFavorite(AppInfo app)
        {
            app.IsFavorite = !app.IsFavorite;
            ConfigService.SaveHistory(History.ToList());
            RefreshDisplayedHistory();
        }

        public void DeleteFromHistory(AppInfo app)
        {
            History.Remove(app);
            ConfigService.SaveHistory(History.ToList());
            RefreshDisplayedHistory();
        }

        public void KillApp(AppInfo app)
        {
            Console.WriteLine($"[Kill Command] Target: {app.Name}, Path: {app.Path}");
            try
            {
                var fileName = app.RealProcessName ?? System.IO.Path.GetFileNameWithoutExtension(app.Path);
                Console.WriteLine($"[Kill Command] Target Name: {fileName}");
                
                // 方案1: 按进程名关闭
                var processes = Process.GetProcessesByName(fileName);
                Console.WriteLine($"[Kill Command] Processes found by name '{fileName}': {processes.Length}");
                
                bool killed = false;
                foreach (var proc in processes)
                {
                    try 
                    { 
                        Console.WriteLine($"[Kill Command] Attempting to kill PID {proc.Id}...");
                        proc.Kill(); 
                        killed = true; 
                        Console.WriteLine($"[Kill Command] PID {proc.Id} killed successfully.");
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Kill Command] Failed to kill PID {proc.Id}: {ex.Message}");
                    }
                }

                // 方案2: 扫描所有进程，匹配主模块路径
                if (!killed)
                {
                    Console.WriteLine("[Kill Command] No processes killed by name, trying path match...");
                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            if (proc.MainModule?.FileName != null && 
                                string.Equals(proc.MainModule.FileName, app.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"[Kill Command] Path match found! PID {proc.Id}. Killing...");
                                proc.Kill();
                                killed = true;
                            }
                        }
                        catch { }
                    }
                }

                if (!killed) Console.WriteLine("[Kill Command] Failed to find any active process to kill.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kill Command] ERROR: {ex.Message}");
            }
        }
    }
}
