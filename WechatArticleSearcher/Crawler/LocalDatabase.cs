using System.Data.SQLite;
using System.Diagnostics;
using WechatArticleSearcher.Properties;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher.Crawler;

/// <summary>
/// 用SQLite数据库在本地保存文章和公众号信息
/// </summary>
public class LocalDatabase : IDataSource
{
    private string _connectionString = $"Data Source={Settings.Default.DbDatabase}.db;Version=3;";

    public LocalDatabase()
    {
        var dbPath = Settings.Default.DbDatabase + ".db";
        var articleTable = Settings.Default.ArticleTable;
        var accountTable = Settings.Default.AccountTable;

        var db = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        db.Open();
        var cmdText = "CREATE TABLE IF NOT EXISTS articles (" +
                      "title_id TEXT PRIMARY KEY," +
                      "article_id TEXT," +
                      "title TEXT," +
                      "url TEXT," +
                      "time TEXT," +
                      "account_id TEXT," +
                      "account_name TEXT" +
                      ")";
        using (var cmd = new SQLiteCommand(cmdText, db))
        {
            cmd.ExecuteNonQuery();
        }

        // todo 表名
        cmdText = "CREATE TABLE IF NOT EXISTS accounts (" +
                  "account_id TEXT PRIMARY KEY," +
                  "name TEXT" +
                  ")";
        using (var cmd = new SQLiteCommand(cmdText, db))
        {
            cmd.ExecuteNonQuery();
        }

        db.Close();
    }

    private SQLiteConnection GetConnection()
    {
        return new SQLiteConnection(_connectionString);
    }

    public void SaveArticle(Article? article, CancellationToken cancelToken = default)
    {
        if (article == null)
        {
            Logging.Warn("Article 为空，跳过保存。");
            return;
        }

        Logging.Info("正在保存文章：" + article.Title);

        var connection = GetConnection();
        connection.Open();
        const string cmdText =
            "INSERT INTO articles (title_id,article_id, title, url, time, account_id, account_name)" +
            " VALUES (@title_id, @article_id, @title, @url, @time, @account_id, @account_name)" +
            " ON CONFLICT (title_id)" +
            " DO UPDATE SET article_id = @article_id, title = @title, url = @url, time = @time";
        using (var cmd = new SQLiteCommand(cmdText, connection))
        {
            Debug.Assert(article != null, nameof(article) + " != null");
            var titleId = Crypto.ComputeSha256Hash(article.Title);
            cmd.Parameters.AddWithValue("title_id", titleId);
            cmd.Parameters.AddWithValue("article_id", article.ArticleId);
            cmd.Parameters.AddWithValue("title", article.Title);
            cmd.Parameters.AddWithValue("url", article.Url);
            cmd.Parameters.AddWithValue("time", DateTimeOffset.FromUnixTimeSeconds(article.Time).DateTime);
            cmd.Parameters.AddWithValue("account_id", article.AccountId);
            cmd.Parameters.AddWithValue("account_name", article.AccountName);
            var retryTimes = 0;
            const int retryLimit = 10;
            while (retryTimes < retryLimit)
            {
                try
                {
                    cmd.ExecuteNonQuery();
                    break;
                }
                catch (SQLiteException e)
                {
                    var randomTime = new Random().Next(100, 500);
                    Logging.Warn($"准备重试第{retryTimes}次，错误信息：{e.Message}");
                    Thread.Sleep(randomTime);
                    retryTimes++;
                }
            }
            if (retryTimes >= retryLimit)
            {
                Logging.Error("重试次数超过限制，保存文章：" + article.Title + "失败。");
            }
        }

        connection.Close();
    }


    public void SaveAccount(Account account, CancellationToken cancelToken = default)
    {
        var connection = GetConnection();
        connection.Open();
        var cmdText = "INSERT INTO accounts (account_id, name) VALUES (@account_id, @name)";
        cmdText += " ON CONFLICT (account_id) DO UPDATE SET name = @name";

        using (var cmd = new SQLiteCommand(cmdText, connection))
        {
            cmd.Parameters.AddWithValue("account_id", account.AccountId);
            cmd.Parameters.AddWithValue("name", account.Name);
            var retryTimes = 0;
            const int retryLimit = 10;
            while (retryTimes < retryLimit)
            {
                try
                {
                    cmd.ExecuteNonQuery();
                    break;
                }
                catch (SQLiteException e)
                {
                    var randomTime = new Random().Next(100, 1000);
                    Logging.Warn(e.Message + "\n重试第：" + retryTimes + " 次。");
                    Thread.Sleep(randomTime);
                    retryTimes++;
                }
            }
        }

        connection.Close();
    }

    public async Task<List<Account>> GetAccounts()
    {
        var connection = GetConnection();
        connection.Open();
        const string cmdText = "SELECT * FROM accounts";
        var accounts = new List<Account>();
        await using var cmd = new SQLiteCommand(cmdText, connection);
        await using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var accountId = reader.GetString(0);
            var name = reader.GetString(1);
            accounts.Add(new Account(accountId, name));
        }

        connection.Close();
        return accounts;
    }

public List<Article> QueryArticles(List<string> accounts, long timeLimit = 0, int numberLimit = 1000,
    List<string>? keywords = null)
{
    var connection = GetConnection();
    connection.Open();

    // 构建 SQL 查询语句
    var cmdText = "SELECT * FROM articles WHERE account_id IN (";
    for (var i = 0; i < accounts.Count; i++)
    {
        cmdText += $"@account{i}";
        if (i != accounts.Count - 1) cmdText += ",";
    }
    cmdText += ")";

    if (timeLimit > 0)
    {
        cmdText += " AND time > @timeLimit";
    }

    if (keywords != null && keywords.Count > 0)
    {
        cmdText += " AND (";
        for (var i = 0; i < keywords.Count; i++)
        {
            cmdText += $"title LIKE @keyword{i}";
            if (i != keywords.Count - 1) cmdText += " OR ";
        }
        cmdText += ")";
    }

    cmdText += " LIMIT @numberLimit";

    using (var cmd = new SQLiteCommand(cmdText, connection))
    {
        // 添加账户 ID 参数
        for (var i = 0; i < accounts.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@account{i}", accounts[i]);
        }

        // 添加时间限制参数
        if (timeLimit > 0)
        {
            cmd.Parameters.AddWithValue("@timeLimit", DateTimeOffset.FromUnixTimeSeconds(timeLimit).DateTime);
        }

        // 添加关键字参数
        if (keywords != null && keywords.Count > 0)
        {
            for (var i = 0; i < keywords.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@keyword{i}", $"%{keywords[i]}%");
            }
        }

        // 添加结果数量限制参数
        cmd.Parameters.AddWithValue("@numberLimit", numberLimit);

        var articles = new List<Article>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var articleId = reader.GetString(1);
                var title = reader.GetString(2);
                var url = reader.GetString(3);
                var time = DateTimeOffset.Parse(reader.GetString(4)).ToUnixTimeSeconds();
                var accountId = reader.GetString(5);
                var accountName = reader.GetString(6);
                articles.Add(new Article(articleId, title, url, time, accountId, accountName));
            }
        }

        connection.Close();
        return articles;
    }
}

    public string GetAccountName(string accountId)
    {
        var connection = GetConnection();
        connection.Open();
        var cmdText = "SELECT name FROM accounts WHERE account_id = @account_id";
        using (var cmd = new SQLiteCommand(cmdText, connection))
        {
            cmd.Parameters.AddWithValue("account_id", accountId);
            var reader = cmd.ExecuteReader();
            reader.Read();
            var name = reader.GetString(0);
            connection.Close();
            return name;
        }
    }
}