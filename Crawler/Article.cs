namespace Crawler;

public class Article
{
    public string ArticleId { get; }
    public string Title { get; }
    public string Url { get; }
    public long Time { get; }
    public string AccountId { get; }
    public string AccountName { get; }
    private Database Database { get; }

    public Article(string aid, string title, string url, long time, string biz, string accountName, Database db)
    {
        ArticleId = aid;
        Title = title;
        Url = url;
        Time = time;
        AccountId = biz;
        AccountName = accountName;
        Database = db;
    }

    public void Update()
    {
        // TODO
        Database.SaveArticle(this);
    }
}