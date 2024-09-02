using System.Windows;
using WechatArticleSearcher.Properties;

namespace WechatArticleSearcher;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();

        DbHost.Text = Settings.Default.DbHost;
        DbPort.Text = Settings.Default.DbPort.ToString();
        ArticleTableName.Text = Settings.Default.ArticleTable;
        AccountTableName.Text = Settings.Default.AccountTable;
        DbName.Text = Settings.Default.DbDatabase;
        DbUser.Text = Settings.Default.DbUser;
        DbPassword.Text = Settings.Default.DbPassword;
        Cookie.Text = Settings.Default.Cookie;
        UserAgent.Text = Settings.Default.UserAgent;
        Token.Text = Settings.Default.Token;
        if (Settings.Default.UseLocalDb)
        {
            LocalDatabaseRadioButton.IsChecked = true;
            RemoteDatabaseRadioButton.IsChecked = false;
        }
        else
        {
            RemoteDatabaseRadioButton.IsChecked = true;
            LocalDatabaseRadioButton.IsChecked = false;
        }
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        Settings.Default.DbHost = DbHost.Text;
        Settings.Default.DbPort = int.Parse(DbPort.Text);
        Settings.Default.DbUser = DbUser.Text;
        Settings.Default.DbPassword = DbPassword.Text;
        Settings.Default.DbDatabase = DbName.Text;
        Settings.Default.Cookie = Cookie.Text;
        Settings.Default.UserAgent = UserAgent.Text;
        Settings.Default.ArticleTable = ArticleTableName.Text;
        Settings.Default.AccountTable = AccountTableName.Text;
        Settings.Default.UseLocalDb = LocalDatabaseRadioButton.IsChecked == true;
        Settings.Default.Token = Token.Text;

        Settings.Default.Save();

        MessageBox.Show("已保存");
        Close();
    }
}