using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.Converters;

// Fills the poll option selection indicator with the accent color when the option is selected.
public sealed class BoolToAccentBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush s_transparentBrush = new(Colors.Transparent);
    private static SolidColorBrush s_accentBrush;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not true) return s_transparentBrush;
        s_accentBrush ??= new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);
        return s_accentBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}