using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace History.Uno.Converters;

public class BoolToPrimaryColorConverter : IValueConverter
{
    private static readonly SolidColorBrush PrimaryBrush = new(Color.FromArgb(0xFF, 0xED, 0x66, 0x4D));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isSelected && isSelected) return PrimaryBrush;
        return new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)); // Transparent
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
