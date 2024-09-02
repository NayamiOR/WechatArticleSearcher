using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WechatArticleSearcher.Crawler;
using WechatArticleSearcher.Properties;

namespace WechatArticleSearcher;

public partial class ArticleSearcherWindow : Window
{
    private readonly IDataSource _database;

    public ArticleSearcherWindow()
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
        AccountListBox.Database = _database;
    }

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var useNumberLimit = UseNumberLimit.IsChecked == true;
        var numberLimit = 1000;
        if (useNumberLimit)
        {
            if (!int.TryParse(NumberLimit.Text, out numberLimit))
            {
                MessageBox.Show("请输入数字");
                return;
            }

            numberLimit = int.Parse(NumberLimit.Text);
        }

        var useTimeLimit = UseTimeLimit.IsChecked == true;
        long timestamp = 0;
        if (useTimeLimit)
        {
            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("请选择时间");
                return;
            }

            var timeLimit = DatePicker.SelectedDate;
            timestamp = new DateTimeOffset(timeLimit!.Value).ToUnixTimeSeconds();
        }


        var useKeywords = UseKeywords.IsChecked == true;
        List<string>? keywords = null;
        if (useKeywords)
            keywords = KeywordsInput.Text.Split(",").Where(x => x != "").ToList();

        var bizs = AccountListBox.ChosenAccounts().Select(account => account.AccountId).ToList();

        var articles = _database.QueryArticles(bizs, timestamp, numberLimit, keywords);

        ArticlesDataGrid.ItemsSource = articles;
        SearchResultLabel.Content = $"共找到{articles.Count}篇文章";
        SearchResultLabel.Visibility = Visibility.Visible;
    }
}