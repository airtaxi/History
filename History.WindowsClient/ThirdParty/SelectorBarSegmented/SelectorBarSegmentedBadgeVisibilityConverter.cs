using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace History.WindowsClient.ThirdParty.SelectorBarSegmented;

/// <summary>
/// Converts a segmented item badge count into visibility, hiding the badge when the count is zero.
/// </summary>
public sealed partial class SelectorBarSegmentedBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
