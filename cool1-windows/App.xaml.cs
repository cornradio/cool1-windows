using System.Configuration;
using System.Data;
using System.Windows;

namespace Cool1Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var themeService = new Services.ThemeService();
        themeService.Initialize();
        base.OnStartup(e);
    }
}

