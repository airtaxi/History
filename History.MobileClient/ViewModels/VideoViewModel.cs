using CommunityToolkit.Mvvm.ComponentModel;

namespace History.MobileClient.ViewModels;

public partial class VideoViewModel(string uri) : ObservableObject, IMediaViewModel
{
    [ObservableProperty]
    public partial string Uri { get; set; } = uri;

    [ObservableProperty]
    public partial Aspect Aspect { get; set; } = Aspect.AspectFill;

    [ObservableProperty]
    public partial bool ResizeParentCarouselViewWhenSizeChanged { get; set; }

    [ObservableProperty]
    public partial LayoutOptions HorizontalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial LayoutOptions VerticalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial bool ShouldAutoPlay { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldLoopPlayback { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldMute { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldShowPlaybackControls { get; set; } = false;

    [ObservableProperty]
    public partial bool ShouldKeepScreenOn { get; set; } = false;

    [ObservableProperty]
    public partial bool FullScreenSwipeable { get; set; } = true;
}

