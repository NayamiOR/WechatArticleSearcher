using Crawler;

namespace TestCrawler;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var database = new Database();
        var httpClient = new HttpClient();
        var crawler = new Crawler.Crawler(httpClient, database);
        // string accountName = await crawler.GetAccountName("https://mp.weixin.qq.com/s/euCWjSzXrYKNgGqazQdOeg");
        crawler.CrawlArticlesAsync(["https://mp.weixin.qq.com/s/euCWjSzXrYKNgGqazQdOeg"]);

        Assert.Pass();
    }
}