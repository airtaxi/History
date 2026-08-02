using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace History.Uno.ViewModels;

public partial class ImageViewModel(string uri) : ObservableObject, IMediaViewModel
{
    [ObservableProperty]
    public partial string Uri { get; set; } = uri;

    [ObservableProperty]
    public partial Stretch Stretch { get; set; } = Stretch.UniformToFill;

    [ObservableProperty]
    public partial HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.Stretch;

    [ObservableProperty]
    public partial VerticalAlignment VerticalContentAlignment { get; set; } = VerticalAlignment.Stretch;

    public double MaxWidth { get; set; } = double.PositiveInfinity;
}
