using System.Globalization;

namespace History.MobileClient.Converters;

public class BoolToUnreadBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUnread && isUnread)
        {
            var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
            return isDark ? Color.FromArgb("#2A2A2A") : Color.FromArgb("#F0F0F0");
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
