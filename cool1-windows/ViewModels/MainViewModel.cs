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
        private string _sortMode = "手动排序"; // 默认排序模式
        private bool _isEditMode;


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

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string SortMode
        {
            get => _sortMode;
            set
            {
                if (SetProperty(ref _sortMode, value))
                {
                    OnPropertyChanged(nameof(IsSortByRecent));
                    OnPropertyChanged(nameof(IsSortByName));
                    OnPropertyChanged(nameof(IsSortByManual));
                    RefreshDisplayedHistory();
                }
            }
        }

        public bool IsSortByRecent
        {
            get => SortMode == "最近启动";
            set { if (value) SortMode = "最近启动"; }
        }

        public bool IsSortByName
        {
            get => SortMode == "名称";
            set { if (value) SortMode = "名称"; }
        }

        public bool IsSortByManual
        {
            get => SortMode == "手动排序";
            set { if (value) SortMode = "手动排序"; }
        }

        public ObservableCollection<AppInfo> DisplayedHistory { get; } = new();

        public ICommand LaunchAppCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand KillCommand { get; }
        public ICommand SelectManualCommand { get; }
        public ICommand LaunchSelectedCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand RunInTerminalCommand { get; }

        public MainViewModel()
        {
            LaunchAppCommand = new RelayCommand(p => { if (p is AppInfo a) LaunchApp(a); });
            ToggleFavoriteCommand = new RelayCommand(p => { if (p is AppInfo a) ToggleFavorite(a); });
            DeleteCommand = new RelayCommand(p => { if (p is AppInfo a) DeleteFromHistory(a); });
            KillCommand = new RelayCommand(p => { if (p is AppInfo a) KillApp(a); });
            SelectManualCommand = new RelayCommand(_ => SelectAppManually());
            LaunchSelectedCommand = new RelayCommand(_ => { if (SelectedApp != null) LaunchApp(SelectedApp); }, _ => SelectedApp != null);
            OpenFolderCommand = new RelayCommand(p => { if (p is AppInfo a) OpenFolder(a); });
            RunInTerminalCommand = new RelayCommand(p => { if (p is AppInfo a) RunInTerminal(a); });

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
                var processGroups = processes.GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                                             .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                foreach (var app in History)
                {
                    bool isRunning = false;
                    string? targetProcName = app.RealProcessName;
                    if (string.IsNullOrEmpty(targetProcName))
                    {
                        targetProcName = System.IO.Path.GetFileNameWithoutExtension(app.Path);
                    }

                    // Handle Chrome/Edge proxy launchers
                    if (targetProcName.EndsWith("_proxy", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetProcName.StartsWith("chrome", StringComparison.OrdinalIgnoreCase)) targetProcName = "chrome";
                        else if (targetProcName.StartsWith("msedge", StringComparison.OrdinalIgnoreCase)) targetProcName = "msedge";
                    }

                    if (processGroups.TryGetValue(targetProcName, out var procs))
                    {
                        // For browser-based apps, we need a more specific check than just "is chrome running"
                        if (targetProcName.Equals("chrome", StringComparison.OrdinalIgnoreCase) || 
                            targetProcName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
                        {
                            // Heuristic: Check window titles for a match with app name
                            isRunning = procs.Any(p => {
                                try {
                                    string title = p.MainWindowTitle;
                                    return !string.IsNullOrEmpty(title) && 
                                           (title.Contains(app.Name, StringComparison.OrdinalIgnoreCase) || 
                                            app.Name.Contains(title, StringComparison.OrdinalIgnoreCase));
                                } catch { return false; }
                            });
                        }
                        else
                        {
                            isRunning = true;
                        }
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
                var detailed = ShortcutService.ResolveShortcutDetailed(app.Path);
                string target = detailed.Path;
                app.Arguments = detailed.Arguments;

                if (string.IsNullOrEmpty(app.RealProcessName))
                {
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
            else if (SortMode == "名称")
            {
                query = query.OrderBy(a => a.Name);
            }
            // "手动排序" 不需要 OrderBy，直接使用 History 的顺序

            DisplayedHistory.Clear();
            foreach (var app in query)
            {
                DisplayedHistory.Add(app);
            }
        }

        public void ReorderItemToPosition(AppInfo source, AppInfo target, bool isTop)
        {
            if (SortMode != "手动排序") return;

            int oldIndex = History.IndexOf(source);
            int targetIndex = History.IndexOf(target);
            if (oldIndex == -1 || targetIndex == -1 || oldIndex == targetIndex) return;

            History.RemoveAt(oldIndex);
            
            // Recalculate target index after removing source
            targetIndex = History.IndexOf(target);
            int newIndex = isTop ? targetIndex : targetIndex + 1;
            
            if (newIndex < 0) newIndex = 0;
            if (newIndex > History.Count) newIndex = History.Count;

            History.Insert(newIndex, source);

            ConfigService.SaveHistory(History.ToList());
            RefreshDisplayedHistory();
        }


        public void LaunchApp(AppInfo app)
        {
            Console.WriteLine($"[Main] Launching: {app.Name} ({app.Path})");
            try
            {
                // 解析快捷方式
                var detailed = ShortcutService.ResolveShortcutDetailed(app.Path);
                string realPath = detailed.Path;
                app.Arguments = detailed.Arguments;

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
                string? fileName = app.RealProcessName;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = System.IO.Path.GetFileNameWithoutExtension(app.Path);
                }

                // Handle proxy
                if (fileName.EndsWith("_proxy", StringComparison.OrdinalIgnoreCase))
                {
                    if (fileName.StartsWith("chrome", StringComparison.OrdinalIgnoreCase)) fileName = "chrome";
                    else if (fileName.StartsWith("msedge", StringComparison.OrdinalIgnoreCase)) fileName = "msedge";
                }

                var processes = Process.GetProcessesByName(fileName);
                bool killed = false;

                foreach (var proc in processes)
                {
                    try 
                    { 
                        bool match = false;
                        // For browsers, only kill if the window title matches (to avoid killing the whole browser)
                        if (fileName.Equals("chrome", StringComparison.OrdinalIgnoreCase) || 
                            fileName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
                        {
                            string title = proc.MainWindowTitle;
                            if (!string.IsNullOrEmpty(title) && 
                                (title.Contains(app.Name, StringComparison.OrdinalIgnoreCase) || 
                                 app.Name.Contains(title, StringComparison.OrdinalIgnoreCase)))
                            {
                                match = true;
                            }
                        }
                        else
                        {
                            match = true; // For regular apps, match by name is enough
                        }

                        if (match)
                        {
                            Console.WriteLine($"[Kill Command] Killing PID {proc.Id} ({proc.ProcessName})...");
                            proc.Kill(); 
                            killed = true; 
                        }
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Kill Command] Failed to kill PID {proc.Id}: {ex.Message}");
                    }
                }

                // Fallback: scanner path match (for non-browsers)
                if (!killed && !fileName.Equals("chrome", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            if (proc.MainModule?.FileName != null && 
                                string.Equals(proc.MainModule.FileName, app.Path, StringComparison.OrdinalIgnoreCase))
                            {
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

        private void OpenFolder(AppInfo app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.Path)) return;
                Console.WriteLine($"[Main] Open Folder: {app.Path}");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{app.Path}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] Open Folder Failed: {ex.Message}");
            }
        }

        private void RunInTerminal(AppInfo app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.Path)) return;
                
                // 解析实际路径，确保 cd 到正确的目录并执行正确的 exe
                string realPath = ShortcutService.ResolveShortcut(app.Path);
                string? dir = System.IO.Path.GetDirectoryName(realPath);
                if (string.IsNullOrEmpty(dir)) return;

                Console.WriteLine($"[Main] Run In Terminal: {realPath} in {dir}");

                // /k keeps the window open
                string args = $"/k \"cd /d \"{dir}\" && \"{realPath}\"\"";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = args,
                    UseShellExecute = true
                };
                Process.Start(psi);
                
                UpdateHistory(app);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] Run In Terminal Failed: {ex.Message}");
                System.Windows.MessageBox.Show($"终端启动失败: {ex.Message}");
            }
        }
    }
}
