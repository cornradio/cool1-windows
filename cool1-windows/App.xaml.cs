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
        // 注册编码提供程序，以支持 GB2312 等编码
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        
        var themeService = new Services.ThemeService();
        themeService.Initialize();
        base.OnStartup(e);
    }
}

