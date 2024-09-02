using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using WechatArticleSearcher.Properties;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher.Crawler;

public class Crawler
{
    private readonly IDataSource _database;
    private readonly int _frequencyPauseTime;
    private readonly HttpClient _httpClient;
    private string _token;

    public Crawler(IDataSource database, Dictionary<string, string> header,
        int frequencyPauseTime = 300)
    {
        _database = database;
        _frequencyPauseTime = frequencyPauseTime;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Clear();
        foreach (var (key, value) in header) _httpClient.DefaultRequestHeaders.Add(key, value);

        _token = Settings.Default.Token;
    }

    /// <summary>
    ///     通过cookie获取token，会直接更新token字段但仍会返回token
    /// </summary>
    /// <returns></returns>
    public string UpdateToken()
    {
        var url = "https://mp.weixin.qq.com/";
        var response = _httpClient.GetStringAsync(url).Result;
        Logging.Info("向服务器请求了一个 Token");
        var pattern = "token=(\\d+)";
        var match = Regex.Match(response, pattern);
        _token = match.Groups[1].Value;
        Settings.Default.Token = _token;
        Settings.Default.Save();
        Logging.Info($"Token 更新成功，当前 Token 为 {_token}");
        return _token;
    }

    public async Task<Article> GetArticle(string link, CancellationToken cancelToken = default)
    {
        var response = await _httpClient.GetStringAsync(link, cancelToken);

        Logging.Info("向服务器爬了一篇文章");

        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        // 获取公众号 biz
        var startIndex = response.IndexOf("var biz = ", StringComparison.Ordinal) + 11;
        var endIndex = response.IndexOf('"', startIndex);
        var biz = response.Substring(startIndex, endIndex - startIndex);

        var name = doc.DocumentNode.SelectSingleNode("//*[@id='js_name']")?.InnerText.Trim() ?? "未知";

        const string pattern = "var\\s+create_time\\s*=\\s*\"(\\d+)\"";
        var match = Regex.Match(response, pattern);
        var createTime = long.Parse(match.Groups[1].Value);

        // 更新公众号信息
        var account = new Account(biz, name);
        _database.SaveAccount(account, cancelToken);

        var article = new Article(
            "-1", // 无法从文章链接获取 ID，因此设置为 -1
            doc.DocumentNode.SelectSingleNode("//*[@id='activity-name']").InnerText.Trim(),
            link,
            createTime,
            biz,
            doc.DocumentNode.SelectSingleNode("//*[@id='js_name']").InnerText.Trim()
        );
        _database.SaveArticle(article, cancelToken);
        Logging.Info($"文章: {article.Title} 爬取成功");

        return article;
    }

    /// <summary>
    ///     根据文章链接批量爬取文章数据，将其和所属公众号加入数据库
    /// </summary>
    /// <param name="links">微信公众号文章链接字符串组成的List</param>
    /// <param name="numOfThreads">爬取使用的线程数（可忽略）</param>
    /// <param name="cancelToken"></param>
    public async Task CrawlArticlesAsync(List<string> links, int numOfThreads = 5,
        CancellationToken cancelToken = default)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(numOfThreads);
        var finishedCount = 0;

        foreach (var link in links)
        {
            try
            {
                await semaphore.WaitAsync(cancelToken);
            }
            catch (OperationCanceledException)
            {
                Logging.Warn("任务被取消");
            }

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await GetArticle(link, cancelToken);
                    finishedCount++;
                    Logging.Info($"完成 {link} 的爬取,还剩{links.Count - finishedCount}个");
                }
                catch (TaskCanceledException)
                {
                    Logging.Warn("任务被取消");
                }
                catch (Exception e)
                {
                    Logging.Error($"爬取文章: {link} 失败: {e.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancelToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            Logging.Info("所有文章爬取完成");
        }
        catch (TaskCanceledException)
        {
            Logging.Warn("批量爬取任务被取消");
        }
    }

    /// <summary>
    ///     根据公众号 biz 爬取一个账号的文章数据，将其和所属公众号加入数据库
    /// </summary>
    /// <param name="biz">一个公众号的biz</param>
    /// <param name="countLimit">数量限制</param>
    /// <param name="timeLimit">时间限制</param>
    /// <param name="cancelToken"></param>
    public async Task CrawlAccountArticles(string biz, int countLimit = 1, long timeLimit = 0,
        CancellationToken cancelToken = default)
    {
        var url = "https://mp.weixin.qq.com/cgi-bin/appmsg";
        const int articlesPerPage = 10;

        var parameters = new Dictionary<string, string>
        {
            { "action", "list_ex" },
            { "begin", "0" },
            { "count", articlesPerPage.ToString() },
            { "fakeid", biz },
            { "type", "9" },
            { "token", _token },
            { "lang", "zh_CN" },
            { "f", "json" },
            { "ajax", "1" }
        };

        var accountName = _database.GetAccountName(biz);
        var i = 0;
        var count = 0;

        while (true)
        {
            parameters["begin"] = (i * articlesPerPage).ToString();
            await Task.Delay(new Random().Next(1, 10) * 5000, cancelToken); // 随机延时避免频控
            var response =
                await _httpClient.GetStringAsync(QueryHelpers.AddQueryString(url, parameters!),
                    cancelToken);
            Logging.Info("向服务器请求了一个公众号的文章列表");

            var json = JsonDocument.Parse(response).RootElement;

            if (json.GetProperty("base_resp").GetProperty("ret").GetInt32() == 200013)
            {
                Logging.Warn($"频率控制，暂停{_frequencyPauseTime}秒");
                await Task.Delay(_frequencyPauseTime * 1000, cancelToken);
                continue;
            }

            if (json.GetProperty("base_resp").GetProperty("ret").GetInt32() == 200040)
            {
                Logging.Warn("Token 失效，更新 Token");
                _token = UpdateToken();
                continue;
            }

            var appMsgList = json.GetProperty("app_msg_list");
            if (appMsgList.GetArrayLength() == 0) break;

            foreach (var item in appMsgList.EnumerateArray())
            {
                var link = item.GetProperty("link").GetString()?.Trim();
                var aid = item.GetProperty("aid").GetString();
                var title = item.GetProperty("title").GetString();
                var createTime = item.GetProperty("create_time").GetInt64();

                if (string.IsNullOrEmpty(link) || string.IsNullOrEmpty(aid) || string.IsNullOrEmpty(title) ||
                    createTime == 0)
                {
                    Logging.Error("文章信息不完整，跳过该条目");
                    continue;
                }

                var article = new Article(aid, title, link, createTime, biz, accountName);
                _database.SaveArticle(article, cancelToken);

                if (++count >= countLimit || createTime < timeLimit) return;
            }

            Logging.Info($"公众号{_database.GetAccountName(biz)}的第{i}页爬取成功");

            i++;
        }
    }

    /// <summary>
    ///     根据公众号 biz 批量爬取多个账号文章数据，将其和所属公众号加入数据库
    /// </summary>
    /// <param name="listOfAccounts"></param>
    /// <param name="countLimit"></param>
    /// <param name="timeLimit"></param>
    /// <param name="numOfThreads"></param>
    /// <param name="cancelToken"></param>
    public async Task UpdateArticlesAsync(List<string> listOfAccounts, int countLimit = 1,
        long timeLimit = 0, int numOfThreads = 5, CancellationToken cancelToken = default)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(numOfThreads);
        var finishedCount = 0;

        foreach (var biz in listOfAccounts)
        {
            try
            {
                await semaphore.WaitAsync(cancelToken);
            }
            catch (TaskCanceledException)
            {
                Logging.Warn("任务被取消");
            }

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await CrawlAccountArticles(biz, countLimit, timeLimit, cancelToken);
                    finishedCount++;
                    Logging.Info($"完成 {biz} 的爬取,还剩{listOfAccounts.Count - finishedCount}个");
                }
                catch (TaskCanceledException)
                {
                    Logging.Warn("任务被取消");
                }
                catch (Exception e)
                {
                    Logging.Error($"爬取公众号 {biz} 失败: {e.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancelToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            Logging.Info("所有公众号爬取完成");
        }
        catch (TaskCanceledException)
        {
            Logging.Warn("批量爬取任务被取消");
        }
    }
}