using System.Windows;
using WechatArticleSearcher.Crawler;
using WechatArticleSearcher.Properties;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher;

public partial class AppendArticleWindow : Window
{
    private readonly Crawler.Crawler _crawler;
    private readonly IDataSource _database;

    public AppendArticleWindow()
    {
        InitializeComponent();
        var useLocalDb = Settings.Default.UseLocalDb;
        if (useLocalDb)
        {
            _database = new LocalDatabase();
        }
        else
        {
            _database = new Database(
                Settings.Default.DbHost,
                Settings.Default.DbPort,
                Settings.Default.DbUser,
                password: Settings.Default.DbPassword,
                database: Settings.Default.DbDatabase,
                articleTable: Settings.Default.ArticleTable,
                accountTable: Settings.Default.AccountTable
            );
        }

        var header = new Dictionary<string, string>
        {
            { "User-Agent", Settings.Default.UserAgent },
            { "Cookie", Settings.Default.Cookie }
        };

        _crawler = new Crawler.Crawler(_database, header);
    }

    private async void AppendArticles(object sender, RoutedEventArgs e)
    {
        var cancelTokenSource = new CancellationTokenSource();
        // 读取输入框中的链接
        var linksBoxText = LinkBox.Text;
        var links = linksBoxText.Split("\n")
            .Where(x => x.StartsWith("http") || x.StartsWith("https"))
            .Where(x => x != "").ToList();

        AppendButton.IsEnabled = false;
        AppendButton.Content = "添加中...";
        var waitingAppendingDialog = new WaitingDialog(cancelTokenSource)
        {
            Owner = this
        };
        waitingAppendingDialog.Show();
        IsEnabled = false;

        Logging.Info("开始批量添加文章");
        // 爬取文章
        await _crawler.CrawlArticlesAsync(links, cancelToken: cancelTokenSource.Token);

        // 关闭等待窗口, 显示提示
        waitingAppendingDialog.Close();
        IsEnabled = true;
        AppendButton.IsEnabled = true;
        AppendButton.Content = "添加";
        MessageBox.Show(cancelTokenSource.Token.IsCancellationRequested ? "添加被取消" : "添加完毕");
    }
}