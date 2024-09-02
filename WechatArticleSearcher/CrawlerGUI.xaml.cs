using System.ComponentModel;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace WechatArticleSearcher;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class CrawlerGui : Window
{
    public CrawlerGui()
    {
        InitializeComponent();

        App.NotifyIcon.Click += (sender, args) =>
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        };
    }



    private void SettingButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSettingWindow(sender, e);
    }

    private void MonitorButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenMonitorWindow(sender, e);
    }

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenUpdatingWindow(sender, e);
    }

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenSearchingWindow(sender, e);
    }

    private void AppendingButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenAppendingWindow(sender, e);
    }

    public static void OpenAppendingWindow(object sender, RoutedEventArgs e)
    {
        var appendArticleWindow = new AppendArticleWindow();
        appendArticleWindow.Show();
    }

    public static void OpenSettingWindow(object sender, RoutedEventArgs e)
    {
        var settingWindow = new SettingWindow();
        settingWindow.Show();
    }

    public static void OpenUpdatingWindow(object sender, RoutedEventArgs e)
    {
        var updateWindow = new UpdateWindow();
        updateWindow.Show();
    }

    public static void OpenMonitorWindow(object sender, RoutedEventArgs e)
    {
        var monitorWindow = new MonitorWindow();
        monitorWindow.Show();
    }

    public static void OpenSearchingWindow(object sender, RoutedEventArgs e)
    {
        var articleSearcherWindow = new ArticleSearcherWindow();
        articleSearcherWindow.Show();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}