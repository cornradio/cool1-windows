using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cool1Windows.Services;
using Cool1Windows.Models;

namespace Cool1Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModels.MainViewModel();
        
        // 加载窗口设置
        LoadWindowSettings();
        
        this.Closing += MainWindow_Closing;
    }

    private void LoadWindowSettings()
    {
        var settings = ConfigService.LoadWindowSettings();
        if (settings != null)
        {
            this.Width = settings.Width;
            this.Height = settings.Height;
            
            if (settings.Left != -1) this.Left = settings.Left;
            if (settings.Top != -1) this.Top = settings.Top;
            
            if (settings.IsMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        var settings = new WindowSettings();
        
        if (this.WindowState == WindowState.Maximized)
        {
            settings.IsMaximized = true;
            settings.Width = this.RestoreBounds.Width;
            settings.Height = this.RestoreBounds.Height;
            settings.Left = this.RestoreBounds.Left;
            settings.Top = this.RestoreBounds.Top;
        }
        else
        {
            settings.IsMaximized = false;
            settings.Width = this.ActualWidth;
            settings.Height = this.ActualHeight;
            settings.Left = this.Left;
            settings.Top = this.Top;
        }

        ConfigService.SaveWindowSettings(settings);
    }

    private void Grid_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Link;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files.Length > 0)
            {
                var viewModel = DataContext as ViewModels.MainViewModel;
                foreach (var file in files)
                {
                    var app = new Models.AppInfo 
                    { 
                        Name = System.IO.Path.GetFileNameWithoutExtension(file),
                        Path = file 
                    };
                    viewModel?.LaunchApp(app);
                }
            }
        }
    }
}