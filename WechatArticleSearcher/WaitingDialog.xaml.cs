using System.Windows;

namespace WechatArticleSearcher;

public partial class WaitingDialog : Window
{
    private CancellationTokenSource TokenSource { get; }

    public WaitingDialog(CancellationTokenSource tokenSource)
    {
        InitializeComponent();
        WindowStyle = WindowStyle.SingleBorderWindow;
        TokenSource = tokenSource;
        ShowInTaskbar = false;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        TokenSource.Cancel();
        Owner.IsEnabled = true;
        Close();
    }
}