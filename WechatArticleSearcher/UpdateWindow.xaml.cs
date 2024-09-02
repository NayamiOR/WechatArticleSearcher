using System.Windows;
using System.Windows.Controls;
using WechatArticleSearcher.Crawler;
using WechatArticleSearcher.Properties;
using WechatArticleSearcher;

namespace WechatArticleSearcher;

public partial class UpdateWindow : Window
{
    private readonly Crawler.Crawler _crawler;
    private readonly IDataSource _database;

    public UpdateWindow()
    {
        InitializeComponent();
        var useLocalDb = Settings.Default.UseLocalDb;
        if (useLocalDb)
            _database = new LocalDatabase();
        else
            _database = new Database(
                Settings.Default.DbHost,
                Settings.Default.DbPort,
                Settings.Default.DbUser,
                password: Settings.Default.DbPassword,
                database: Settings.Default.DbDatabase,
                articleTable: Settings.Default.ArticleTable,
                accountTable: Settings.Default.AccountTable
            );

        var header = new Dictionary<string, string>
        {
            { "User-Agent", Settings.Default.UserAgent },
            { "Cookie", Settings.Default.Cookie }
        };
        _crawler = new Crawler.Crawler(_database, header);
        AccountListBox.Database = _database;
    }


    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        var cancelTokenSource = new CancellationTokenSource();
        var accounts = AccountListBox.ChosenAccounts().Select(account => account.AccountId).ToList();

        var numberLimit = UseNumberLimit.IsChecked == true ? int.Parse(NumberLimit.Text) : 1000;
        var timeLimit = UseTimeLimit.IsChecked == true ? DatePicker.SelectedDate : null;
        var timestamp = timeLimit is null
            ? 0
            : new DateTimeOffset(timeLimit.Value).ToUnixTimeSeconds();
        UpdateButton.Content = "更新中";
        UpdateButton.IsEnabled = false;

        var waitingDialog = new WaitingDialog(cancelTokenSource)
        {
            Owner = this
        };
        waitingDialog.Show();
        IsEnabled = false;
        await _crawler.UpdateArticlesAsync(accounts, numberLimit, timestamp,
            cancelToken: cancelTokenSource.Token);
        waitingDialog.Close();
        IsEnabled = true;
        UpdateButton.Content = "更新";
        UpdateButton.IsEnabled = true;
        MessageBox.Show(cancelTokenSource.Token.IsCancellationRequested ? "更新被取消" : "更新完毕");
    }
}