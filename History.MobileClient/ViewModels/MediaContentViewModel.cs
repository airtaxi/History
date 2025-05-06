using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class MediaContentViewModel : ObservableObject, IContentViewModel
{
    public IEnumerable<MediaContent> AllMediaContents { get; }

    public MediaContent MediaContent { get; }
    public bool IsWrapped { get; }
    public bool IsVideo { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial IMediaViewModel Media { get; private set; }

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, bool isWrapped)
    {
        AllMediaContents = allMediaContents;
        MediaContent = mediaContent;
        IsWrapped = isWrapped;
        IsVideo = mediaContent.IsVideo;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = !string.IsNullOrEmpty(Description);
        GenerateMedia();
    }

    [RelayCommand]
    public void Unloaded()
    {
        if (!MediaContent.IsVideo) return;

        GenerateMedia();
    }

    [RelayCommand]
    public void HandleOverlayTap()
    {
        if (!MediaContent.IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

        IsOverlayVisible = false;
        Media = new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFill,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill
        };
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        IMediaViewModel viewModel = MediaContent.IsVideo ?
        new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            ShouldAutoPlay = true,
            ShouldLoopPlayback = true,
            ShouldMute = false,
            ShouldShowPlaybackControls = true
        }
        : new ImageViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            IsFullScreen = true
        };

        var viewerPage = new FullScreenMediaViewerPage(viewModel);
        await App.PushModalAsync(viewerPage);
    }

    private void GenerateMedia()
    {
        Media = new ImageViewModel(Utils.GenerateMediaUri((IsWrapped || MediaContent.IsVideo) ? MediaContent.ThumbnailMediaId : MediaContent.MediaId))
        {
            Aspect = IsWrapped ? Aspect.AspectFill : Aspect.AspectFit,
            HorizontalContentOptions = IsWrapped || MediaContent.IsVideo ? LayoutOptions.Fill : LayoutOptions.Start,
            VerticalContentOptions = IsWrapped || MediaContent.IsVideo ? LayoutOptions.Fill : LayoutOptions.Start,
        };
        IsOverlayVisible = MediaContent.IsVideo;
    }
}
