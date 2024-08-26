using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;

namespace Crawler
{
    public class Crawler
    {
        private HttpClient _httpClient;
        private Database _database;
        private string _token;
        private int _frequencyPauseTime;

        public Crawler(HttpClient httpClient, Database database, int frequencyPauseTime = 300)
        {
            _httpClient = httpClient;
            _database = database;
            _frequencyPauseTime = frequencyPauseTime;
            _token = GetToken().Result;
        }

        private async Task<string> GetToken()
        {
            var url = "https://mp.weixin.qq.com";
            var response = await _httpClient.GetStringAsync(url);
            var match = Regex.Match(response, @"token=(\d+)");
            return match.Success ? match.Groups[1].Value : "获取失败，Cookie可能已经过期，请重新获取";
        }

        public async Task CrawlArticlesByCountAsync(List<string> listOfAccounts, int countLimit = 100,
            int numOfThreads = 5)
        {
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(numOfThreads);

            foreach (var biz in listOfAccounts)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await CrawlAccountArticlesByCount(biz, countLimit);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        private async Task CrawlAccountArticlesByCount(string biz, int countLimit)
        {
            var url = "https://mp.weixin.qq.com/cgi-bin/appmsg";
            var parameters = new Dictionary<string, string>
            {
                { "action", "list_ex" },
                { "count", "5" },
                { "fakeid", biz },
                { "type", "9" },
                { "token", _token },
                { "lang", "zh_CN" },
                { "f", "json" },
                { "ajax", "1" }
            };

            string accountName = null;
            int i = 0;
            int count = 0;

            while (true)
            {
                parameters["begin"] = (i * 5).ToString();
                await Task.Delay(new Random().Next(1, 10) * 5000); // 随机延时避免频控
                var response = await _httpClient.GetStringAsync(QueryHelpers.AddQueryString(url, parameters));

                var json = JsonDocument.Parse(response).RootElement;
                if (json.GetProperty("base_resp").GetProperty("ret").GetInt32() == 200013)
                {
                    Console.WriteLine($"频率控制，暂停{_frequencyPauseTime}秒");
                    await Task.Delay(_frequencyPauseTime * 1000);
                    continue;
                }

                var appMsgList = json.GetProperty("app_msg_list");
                if (appMsgList.GetArrayLength() == 0) break;

                foreach (var item in appMsgList.EnumerateArray())
                {
                    if (count > countLimit) break;

                    if (accountName == null)
                    {
                        accountName = await GetAccountName(item.GetProperty("link").GetString());
                        var account = new Account(biz, accountName, _database);
                        account.Update();
                    }

                    var article = new Article(
                        item.GetProperty("aid").GetString(),
                        item.GetProperty("title").GetString(),
                        item.GetProperty("link").GetString(),
                        item.GetProperty("create_time").GetInt64(),
                        biz,
                        accountName,
                        _database
                    );
                    article.Update();

                    Console.WriteLine($"第{i}页爬取成功");
                    Console.WriteLine(
                        "\n--------------------------------------------------------------------------------\n");
                    count++;
                }

                i++;
            }
        }

        /// <summary>
        /// 从文章链接中获取公众号名称
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<string> GetAccountName(string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            var element = doc.DocumentNode.SelectSingleNode("//*[@id='js_name']");
            return element?.InnerText.Trim() ?? "未知";
        }

        public string CrawlBiz(string link)
        {
            var response = _httpClient.GetStringAsync(link).Result;
            var startIndex = response.IndexOf("__biz=") + 6;
            var endIndex = response.IndexOf("&", startIndex);
            return response.Substring(startIndex, endIndex - startIndex);
        }

        public async Task CrawlAccountsFromLinksByCountAsync(List<string> links, int countLimit, int numOfThreads = 5)
        {
            var bizList = new List<string>();
            foreach (var link in links)
            {
                bizList.Add(CrawlBiz(link));
            }

            await CrawlArticlesByCountAsync(bizList, countLimit, numOfThreads);
        }

        public async Task CrawlAccountsFromLinksByTimeAsync(List<string> links, long timeLimit, int numOfThreads = 5)
        {
            var bizList = new List<string>();
            foreach (var link in links)
            {
                bizList.Add(CrawlBiz(link));
            }

            await CrawlArticlesByTimeAsync(bizList, timeLimit, numOfThreads);
        }

        private async Task CrawlArticlesByTimeAsync(List<string> listOfAccounts, long timeLimit, int numOfThreads = 5)
        {
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(numOfThreads);

            foreach (var biz in listOfAccounts)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await CrawlAccountArticlesByTime(biz, timeLimit);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        private async Task CrawlAccountArticlesByTime(string biz, long timeLimit)
        {
            var url = "https://mp.weixin.qq.com/cgi-bin/appmsg";
            var parameters = new Dictionary<string, string>
            {
                { "action", "list_ex" },
                { "count", "5" },
                { "fakeid", biz },
                { "type", "9" },
                { "token", _token },
                { "lang", "zh_CN" },
                { "f", "json" },
                { "ajax", "1" }
            };

            string accountName = null;
            int i = 0;

            while (true)
            {
                parameters["begin"] = (i * 5).ToString();
                await Task.Delay(new Random().Next(1, 10) * 5000);
                var response = await _httpClient.GetStringAsync(QueryHelpers.AddQueryString(url, parameters));

                var json = JsonDocument.Parse(response).RootElement;
                if (json.GetProperty("base_resp").GetProperty("ret").GetInt32() == 200013)
                {
                    Console.WriteLine($"频率控制，暂停{_frequencyPauseTime}秒");
                    await Task.Delay(_frequencyPauseTime * 1000);
                    continue;
                }

                var appMsgList = json.GetProperty("app_msg_list");
                if (appMsgList.GetArrayLength() == 0) break;

                foreach (var item in appMsgList.EnumerateArray())
                {
                    if (item.GetProperty("create_time").GetInt64() > timeLimit) break;

                    if (accountName == null)
                    {
                        accountName = await GetAccountName(item.GetProperty("link").GetString());
                        var account = new Account(biz, accountName, _database);
                        account.Update();
                    }

                    var article = new Article(
                        item.GetProperty("aid").GetString(),
                        item.GetProperty("title").GetString(),
                        item.GetProperty("link").GetString(),
                        item.GetProperty("create_time").GetInt64(),
                        biz,
                        accountName,
                        _database
                    );
                    article.Update();

                    Console.WriteLine($"第{i}页爬取成功");
                    Console.WriteLine(
                        "\n--------------------------------------------------------------------------------\n");
                }

                i++;
            }
        }

        public async Task<Article> GetArticleData(string link)
        {
            var response = await _httpClient.GetStringAsync(link);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var article = new Article(
                aid: "-1", // 无法从文章链接获取 ID，因此设置为 -1
                title: doc.DocumentNode.SelectSingleNode("//*[@id='activity-name']").InnerText.Trim(),
                url: link,
                time: DateTimeOffset.Now.ToUnixTimeSeconds(), // 用当前时间模拟文章时间
                biz: CrawlBiz(link),
                accountName: doc.DocumentNode.SelectSingleNode("//*[@id='js_name']").InnerText.Trim(),
                db: _database
            );

            return article;
        }

        public async Task CrawlArticlesAsync(List<string> links, int numOfThreads = 5)
        {
            foreach (var link in links)
            {
                var biz = CrawlBiz(link);
                var name = await GetAccountName(link);
                var account = new Account(biz, name, _database);
                account.Update();

                var article = await GetArticleData(link);
                article.Update();

                await Task.Delay(new Random().Next(0, 2) * 10000);
            }
        }
    }
}