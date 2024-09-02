using System.Globalization;
using System.Windows.Data;

namespace WechatArticleSearcher;

public class TimestampToDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long timestamp)
        {
            // Convert the timestamp to DateTime
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}