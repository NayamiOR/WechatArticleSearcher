using Microsoft.Extensions.Logging;

namespace Crawler;

public class Account
{
    public string AccountId { get; }
    public string Name { get; }
    private Database Database { get; }

    public Account(string biz, string name, Database db)
    {
        AccountId = biz;
        Name = name;
        Database = db;
    }

    public void Update()
    {
        // TODO
        Database.SaveAccount(this);
    }
}