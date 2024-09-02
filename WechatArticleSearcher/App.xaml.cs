using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using log4net.Config;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static NotifyIcon NotifyIcon { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Logging.Init();

        var iconUri = new Uri("pack://application:,,,/Resources/icon.ico");
        var icon = new BitmapImage(iconUri);

        NotifyIcon = new NotifyIcon
        {
            Text = "Wechat Article Searcher",
            Icon = icon,
            Visibility = Visibility.Visible
        };

        var contextMenu = new ContextMenu();
        var appendMenuItem = new MenuItem { Header = "添加文章" };
        appendMenuItem.Click += CrawlerGui.OpenAppendingWindow;
        contextMenu.Items.Add(appendMenuItem);
        var settingMenuItem = new MenuItem { Header = "设置" };
        settingMenuItem.Click += CrawlerGui.OpenSettingWindow;
        contextMenu.Items.Add(settingMenuItem);
        var monitorMenuItem = new MenuItem { Header = "监控" };
        monitorMenuItem.Click += CrawlerGui.OpenMonitorWindow;
        contextMenu.Items.Add(monitorMenuItem);
        var updateMenuItem = new MenuItem { Header = "更新文章" };
        updateMenuItem.Click += CrawlerGui.OpenUpdatingWindow;
        contextMenu.Items.Add(updateMenuItem);
        var searchMenuItem = new MenuItem { Header = "检索文章" };
        searchMenuItem.Click += CrawlerGui.OpenSearchingWindow;
        contextMenu.Items.Add(searchMenuItem);
        var exitMenuItem = new MenuItem { Header = "退出" };
        exitMenuItem.Click += (o, eventArgs) =>
        {
            NotifyIcon.Dispose();
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitMenuItem);

        NotifyIcon.ContextMenu = contextMenu;

        NotifyIcon.MouseRightButtonDown += (sender, args) => { contextMenu.IsOpen = true; };

        NotifyIcon.Init();

    }

    protected override void OnExit(ExitEventArgs e)
    {
        NotifyIcon.Dispose();
        base.OnExit(e);
    }
}