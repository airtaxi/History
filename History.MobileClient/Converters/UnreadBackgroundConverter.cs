using System.Globalization;

namespace History.MobileClient.Converters;

public class BoolToUnreadBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUnread && isUnread)
        {
            var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
            return isDark ? Color.FromArgb("#333333") : Color.FromArgb("#FBE0DB");
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
