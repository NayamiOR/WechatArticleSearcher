namespace Crawler;

public class Database
{
    public Database()
    {
        var accountPath = @"E:\Home\Temp Files\Account.csv";
        if (!File.Exists(accountPath))
        {
            File.Create(accountPath).Close();
            // write header
            using (var writer = new StreamWriter(accountPath))
            {
                writer.WriteLine("AccountId,Name");
            }
        }

        var articlePath = @"E:\Home\Temp Files\Article.csv";
        if (!File.Exists(articlePath))
        {
            File.Create(articlePath).Close();
            // write header
            using (var writer = new StreamWriter(articlePath))
            {
                writer.WriteLine("ArticleId,Title,Url,Time,AccountId,AccountName");
            }
        }
    }

    public async void SaveArticle(Article article)
    {
        // append to file
        var article_path = @"E:\Home\Temp Files\Article.csv";
        var article_data = new FileInfo(article_path);
        using (var writer = new StreamWriter(article_data.FullName, true))
        {
            await writer.WriteLineAsync($"{article.ArticleId},{article.Title},{article.Url},{article.Time},{article.AccountId},{article.AccountName}");
        }
    }

    public async void SaveAccount(Account account)
    {
        // append to file
        var account_path = @"E:\Home\Temp Files\Account.csv";
        var account_data = new FileInfo(account_path);
        using (var writer = new StreamWriter(account_data.FullName, true))
        {
            await writer.WriteLineAsync($"{account.AccountId},{account.Name}");
        }
    }
}