using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Pages;
using Newtonsoft.Json.Converters;

namespace History.MobileClient.ViewModels;

public partial class MediaContentViewModel : ObservableObject, IContentViewModel
{
    public MediaContent MediaContent { get; }
    public bool IsTimeline { get; }
    public bool IsVideo { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial IMediaViewModel Media { get; private set; }

    private readonly List<IMediaViewModel> _fullScreenMedias;
    private readonly IMediaViewModel _currentMedia;

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, bool isTimeline)
    {
        MediaContent = mediaContent;
        IsTimeline = isTimeline;
        IsVideo = mediaContent.IsVideo;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = !string.IsNullOrEmpty(Description);

        var index = allMediaContents.ToList().FindIndex(x => x.MediaId == mediaContent.MediaId);
        _fullScreenMedias = [.. allMediaContents.Select(x => GenerateFullScreenMedia(x))];
        _currentMedia = _fullScreenMedias[index];

        SetMediaAndOverlay();
    }

    [RelayCommand]
    public void Unloaded()
    {
        if (!MediaContent.IsVideo) return;

        SetMediaAndOverlay();
#if IOS
        WeakReferenceMessenger.Default.Send(new AppleVideoUnloadedMessage());
#endif
    }

    [RelayCommand]
#if ANDROID
    public void HandleOverlayTap()
#elif IOS
    public async Task HandleOverlayTap()
#endif
    {
        if (!MediaContent.IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

#if ANDROID
        IsOverlayVisible = false;
        Media = new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFill,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill
        };
#elif IOS
        if (IsTimeline)
        {
            var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(_fullScreenMedias, _currentMedia));
            await App.PushModalAsync(viewerPage);

            await Toast.Make("iOS에서는 현재 타임라인에서 영상 재생이 지원되지 않습니다.").Show();
        }
        else
        {
            IsOverlayVisible = false;
            Media = new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
            {
                Aspect = Aspect.AspectFill,
                HorizontalContentOptions = LayoutOptions.Fill,
                VerticalContentOptions = LayoutOptions.Fill
            };
        }
#endif
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(_fullScreenMedias, _currentMedia));
        await App.PushModalAsync(viewerPage);
    }

    private void SetMediaAndOverlay()
    {
        Media = new ImageViewModel(Utils.GenerateMediaUri((IsTimeline || MediaContent.IsVideo) ? MediaContent.ThumbnailMediaId : MediaContent.MediaId))
        {
            Aspect = IsTimeline ? Aspect.AspectFill : Aspect.AspectFit,
            ResizeParentCarouselViewWhenSizeChanged = !IsTimeline
        };
        IsOverlayVisible = MediaContent.IsVideo;
    }

    private static IMediaViewModel GenerateFullScreenMedia(MediaContent mediaContent)
    {
        return mediaContent.IsVideo ?
        new VideoViewModel(Utils.GenerateMediaUri(mediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            ShouldAutoPlay = true,
            ShouldLoopPlayback = true,
            ShouldMute = false,
            ShouldShowPlaybackControls = true
        }
        : new ImageViewModel(Utils.GenerateMediaUri(mediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            IsFullScreen = true
        };
    }
}
