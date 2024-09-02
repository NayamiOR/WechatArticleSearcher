namespace WechatArticleSearcher.Crawler;

public class Account
{
    public Account(string biz, string name)
    {
        AccountId = biz;
        Name = name;
    }

    public string AccountId { get; }
    public string Name { get; }
}