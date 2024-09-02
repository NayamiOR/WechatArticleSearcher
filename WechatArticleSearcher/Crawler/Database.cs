using Npgsql;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher.Crawler;

public class Database : IDataSource
{
    private readonly string _accountTable;
    private readonly string _articleTable;
    private readonly string _database;
    private readonly string _host;
    private readonly string _password;
    private readonly int _port;
    private readonly string _username;

    public Database(string host, int port, string username, string database, string password,
        string articleTable = "articles",
        string accountTable = "accounts")
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _database = database;
        _articleTable = articleTable;
        _accountTable = accountTable;
    }

    public async void SaveArticle(Article? article, CancellationToken cancelToken = default)
    {
        var titleId = Crypto.ComputeSha256Hash(article.Title);
        await using var connection = new NpgsqlConnection(
            $"Host={_host};Port={_port};Username={_username};Password={_password};Database={_database}");
        await connection.OpenAsync(cancelToken);

        var cmdText =
            $"INSERT INTO {_articleTable} (title_id,article_id, title, url, time, account_id, account_name)" +
            $" VALUES (@title_id, @article_id, @title, @url, @time, @account_id, @account_name)" +
            $" ON CONFLICT (title_id)" +
            $" DO UPDATE SET article_id = @article_id, title = @title, url = @url, time = @time";

        using (var cmd = new NpgsqlCommand(cmdText, connection))
        {
            cmd.Parameters.AddWithValue("title_id", titleId);
            cmd.Parameters.AddWithValue("article_id", article.ArticleId);
            cmd.Parameters.AddWithValue("title", article.Title);
            cmd.Parameters.AddWithValue("url", article.Url);
            cmd.Parameters.AddWithValue("time",
                DateTimeOffset.FromUnixTimeSeconds(article.Time).DateTime);
            cmd.Parameters.AddWithValue("account_id", article.AccountId);
            cmd.Parameters.AddWithValue("account_name", article.AccountName);
            await cmd.ExecuteNonQueryAsync(cancelToken);
        }

        await connection.CloseAsync();
    }

    public async void SaveAccount(Account account, CancellationToken cancelToken = default)
    {
        var connection = new NpgsqlConnection(
            $"Host={_host};Port={_port};Username={_username};Password={_password};Database={_database}");


        await connection.OpenAsync(cancelToken);

        var cmd = new NpgsqlCommand(
            $"INSERT INTO {_accountTable} (account_id, account_name) VALUES (@account_id, @account_name) " +
            $"ON CONFLICT (account_id) DO NOTHING", // TODO: 如果已经存在则更新信息
            connection);
        cmd.Parameters.AddWithValue("account_id", account.AccountId);
        cmd.Parameters.AddWithValue("account_name", account.Name);
        await cmd.ExecuteNonQueryAsync(cancelToken);
        await connection.CloseAsync();
    }

    public string GetAccountName(string accountId)
    {
        NpgsqlConnection connection;
        connection = new NpgsqlConnection(
            $"Host={_host};Port={_port};Username={_username};Password={_password};Database={_database}");

        connection.Open();

        var cmd = new NpgsqlCommand(
            $"SELECT account_name FROM {_accountTable} WHERE account_id = @account_id",
            connection);
        cmd.Parameters.AddWithValue("account_id", accountId);
        var reader = cmd.ExecuteReader();
        reader.Read();
        var name = reader.GetString(0);
        connection.Close();
        return name;
    }

    public async Task<List<Account>> GetAccounts()
    {
        NpgsqlConnection connection;
        connection = new NpgsqlConnection(
            $"Host={_host};Port={_port};Username={_username};Password={_password};Database={_database}");

        connection.Open();

        var cmd = new NpgsqlCommand($"SELECT * FROM {_accountTable}", connection);
        var reader = await cmd.ExecuteReaderAsync();
        var accounts = new List<Account>();
        while (await reader.ReadAsync())
            accounts.Add(new Account(reader.GetString(0), reader.GetString(1)));

        await connection.CloseAsync();
        return accounts;
    }

    public List<Article> QueryArticles(List<string> accounts, long timeLimit = 0,
        int numberLimit = 1000,
        List<string>? keywords = null)
    {
        List<Article> articlesReturning = new();

        // 创建数据库连接
        using (var connection = new NpgsqlConnection(
                   $"Host={_host};Port={_port};Username={_username};Password={_password};Database={_database}"))
        {
            connection.Open();

            if (accounts.Count == 0) return articlesReturning;

            // 构建基本的 SQL 查询
            var cmd = "SELECT * FROM articles WHERE account_id IN (";
            for (var i = 0; i < accounts.Count; i++)
            {
                cmd += $"@account{i}";
                if (i != accounts.Count - 1)
                    cmd += ",";
            }

            cmd += ")";

            // 添加时间限制
            if (timeLimit != 0)
                cmd += " AND time > to_timestamp(@timeLimit)";

            // 添加关键字过滤
            if (keywords is { Count: > 0 })
            {
                cmd += " AND (";
                for (var i = 0; i < keywords.Count; i++)
                {
                    cmd += $"title LIKE @keyword{i}";
                    if (i != keywords.Count - 1)
                        cmd += " OR ";
                }

                cmd += ")";
            }

            // 添加排序
            cmd += " ORDER BY time DESC";

            // 添加结果数量限制
            cmd += " LIMIT @numberLimit";

            using (var command = new NpgsqlCommand(cmd, connection))
            {
                // 添加账户 ID 参数
                for (var i = 0; i < accounts.Count; i++)
                    command.Parameters.AddWithValue($"@account{i}", accounts[i]);

                // 添加时间限制参数
                if (timeLimit != 0)
                    command.Parameters.AddWithValue("@timeLimit", timeLimit);

                // 添加关键字参数
                if (keywords != null && keywords.Count > 0)
                    for (var i = 0; i < keywords.Count; i++)
                        command.Parameters.AddWithValue($"@keyword{i}", $"%{keywords[i]}%");

                // 添加结果数量限制参数
                if (numberLimit < 0) numberLimit = 0;

                command.Parameters.AddWithValue("@numberLimit", numberLimit);

                // 执行查询并读取结果
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        articlesReturning.Add(new Article(
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            new DateTimeOffset(reader.GetDateTime(4)).ToUnixTimeSeconds(),
                            reader.GetString(5),
                            reader.GetString(6)));
                }
            }
            connection.Close();
        }

        return articlesReturning;
    }

}