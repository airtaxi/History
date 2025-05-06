namespace History.MobileClient.ViewModels;

public partial class FullScreenVideoViewModel : VideoViewModel
{
    public FullScreenVideoViewModel(VideoViewModel source) : base(source.Uri, source.Description)
    {
        Aspect = Aspect.AspectFit;
        VideoShouldAutoPlay = true;
        VideoShouldLoopPlayback = true;
        ShouldMute = false;
        VideoShouldShowPlaybackControls = true;
    }
}

