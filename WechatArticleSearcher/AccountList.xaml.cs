using System.Windows;
using System.Windows.Controls;
using WechatArticleSearcher.Crawler;
using WechatArticleSearcher.Utils;

namespace WechatArticleSearcher;

public partial class AccountList : UserControl
{
    // items
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        "Items", typeof(IEnumerable<Account>), typeof(AccountList),
        new PropertyMetadata(default(IEnumerable<Account>)));

    // ItemsSource
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource", typeof(IEnumerable<Account>), typeof(AccountList),
        new PropertyMetadata(default(IEnumerable<Account>)));

    public IEnumerable<Account> Items
    {
        get => (IEnumerable<Account>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public IEnumerable<Account> ItemsSource
    {
        get => (IEnumerable<Account>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataSource Database { get; set; }

    public AccountList()
    {
        InitializeComponent();
        Loaded += LoadAccounts;
    }

    public List<Account> ChosenAccounts()
    {
        var accounts = new List<Account>();
        foreach (var item in AccountListBox.Items)
        {
            var container =
                AccountListBox.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            var checkBox =
                container?.ContentTemplate.FindName("CheckBox", container) as CheckBox;
            if (checkBox!.IsChecked == true)
            {
                accounts.Add(item as Account);
            }
        }

        return accounts;
    }

    private void SelectAllAccountCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        foreach (var item in AccountListBox.Items)
        {
            var container =
                AccountListBox.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            var checkBox =
                container?.ContentTemplate.FindName("CheckBox", container) as CheckBox;
            checkBox!.IsChecked = true;
        }
    }

    private void SelectAllAccountCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        foreach (var item in AccountListBox.Items)
        {
            var container =
                AccountListBox.ItemContainerGenerator.ContainerFromItem(item) as
                    ContentPresenter;
            var checkBox =
                container?.ContentTemplate.FindName("CheckBox", container) as CheckBox;
            checkBox!.IsChecked = false;
        }
    }

    private void Account_OnUnchecked(object sender, RoutedEventArgs e)
    {
        SelectAllAccountCheckBox.Unchecked -= SelectAllAccountCheckBox_OnUnchecked;
        SelectAllAccountCheckBox.IsChecked = false;
        SelectAllAccountCheckBox.Unchecked += SelectAllAccountCheckBox_OnUnchecked;
    }

    private async void LoadAccounts(object sender, RoutedEventArgs e)
    {
        var accounts = await Database.GetAccounts();
        AccountListBox.ItemsSource = accounts;
    }
}