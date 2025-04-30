using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class VideoViewModel(string uri) : ObservableObject, IMediaViewModel
{
    [ObservableProperty]
    public partial string Uri { get; set; } = uri;

    [ObservableProperty]
    public partial Aspect Aspect { get; set; } = Aspect.AspectFill;

    [ObservableProperty]
    public partial LayoutOptions HorizontalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial LayoutOptions VerticalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial bool VideoShouldAutoPlay { get; set; } = true;

    [ObservableProperty]
    public partial bool VideoShouldLoopPlayback { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldMute { get; set; } = true;

    [ObservableProperty]
    public partial bool VideoShouldShowPlaybackControls { get; set; } = false;
}

