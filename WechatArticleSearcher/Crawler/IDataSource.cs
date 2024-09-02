namespace WechatArticleSearcher.Crawler;

public interface IDataSource
{
    void SaveArticle(Article? article,CancellationToken cancelToken=default);
    void SaveAccount(Account account,CancellationToken cancelToken=default);
    string GetAccountName(string accountId);

    Task<List<Account>> GetAccounts();

    List<Article> QueryArticles(List<string> accounts, long timeLimit = 0,
        int numberLimit = 1000,
        List<string>? keywords = null);
}