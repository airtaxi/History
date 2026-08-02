using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;

namespace History.Uno.ViewModels;

public partial class VideoViewModel(string uri) : ObservableObject, IMediaViewModel
{
    public MediaSource Source { get; } = MediaSource.CreateFromUri(new Uri(uri));

    [ObservableProperty]
    public partial string Uri { get; set; } = uri;

    [ObservableProperty]
    public partial Stretch Stretch { get; set; } = Stretch.UniformToFill;

    [ObservableProperty]
    public partial HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.Stretch;

    [ObservableProperty]
    public partial VerticalAlignment VerticalContentAlignment { get; set; } = VerticalAlignment.Stretch;

    [ObservableProperty]
    public partial bool ShouldAutoPlay { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldLoopPlayback { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldMute { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldShowPlaybackControls { get; set; }

    public double MaxWidth { get; set; } = double.PositiveInfinity;
}
