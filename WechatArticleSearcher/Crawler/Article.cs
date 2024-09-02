namespace WechatArticleSearcher.Crawler;

public class Article
{
    public Article(string aid, string title, string url, long time, string biz, string accountName)
    {
        ArticleId = aid;
        Title = title;
        Url = url;
        Time = time;
        AccountId = biz;
        AccountName = accountName;
    }

    public string ArticleId { get; }
    public string Title { get; }
    public string Url { get; }
    public long Time { get; }
    public string AccountId { get; }
    public string AccountName { get; }
}