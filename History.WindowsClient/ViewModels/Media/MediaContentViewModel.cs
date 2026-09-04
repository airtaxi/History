using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels;
using History.WindowsClient.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Core;

namespace History.WindowsClient.ViewModels.Media;

// Carousel item view model for a single media content. The inline surface is image-first
// (thumbnail for wrapped posts and videos), tapping the video overlay starts inline playback,
// and tapping the media itself opens the full-screen MediaWindow. Full-screen instances
// (IsFullScreen) are built by the MediaWindowViewModel with the original image (or the video
// thumbnail), Uniform stretch, and no cache warm-up.
public sealed partial class MediaContentViewModel : ObservableObject
{
    public MediaContent MediaContent { get; }
    public List<MediaContent> AllMediaContents { get; }
    public PostType PostType { get; }
    public bool IsParentPost { get; }
    public bool IsVideo { get; }
    public bool IsSpoiler { get; }
    public bool IsFullScreen { get; }
    public Stretch Stretch { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    // The media id the inline image is served from: the thumbnail for wrapped posts and videos,
    // the original media for unwrapped image posts. Also drives the MediaCacheService lookups.
    public string InlineImageMediaId { get; }

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }
    public bool HasPixelSize { get; private set; }

    [ObservableProperty]
    public partial BitmapImage ImageSource { get; private set; }

    [ObservableProperty]
    public partial double Width { get; set; } = double.NaN;

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsSpoilerOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; private set; }

    [ObservableProperty]
    public partial MediaSource PlaybackMediaSource { get; private set; }

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost, bool isFullScreen = false)
    {
        MediaContent = mediaContent;
        AllMediaContents = allMediaContents == null ? [] : [.. allMediaContents];
        PostType = postType;
        IsParentPost = isParentPost;
        IsVideo = mediaContent.IsVideo;
        IsSpoiler = mediaContent.IsSpoiler;
        IsFullScreen = isFullScreen;
        IsOverlayVisible = IsVideo;
        IsSpoilerOverlayVisible = IsSpoiler;
        Stretch = isFullScreen || postType == PostType.Unwrapped ? Stretch.Uniform : Stretch.UniformToFill;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = Description.Length > 0;
        InlineImageMediaId = GetInlineImageMediaId(mediaContent, postType, isFullScreen);

        // Cache hit: load from the disk cache with its pre-recorded pixel dimensions, so the
        // carousel gets its final height on the first measure pass. Cache miss: fall back to
        // the network URI now (the ImageOpened event reports the size later) and let the
        // background download cache the bitmap for the next realization.
        ImageSource = CreateInlineImageSource(mediaContent, postType);
        if (!isFullScreen) _ = InitializeFromCacheAsync();
    }

    // Warm path: swap the network-backed image source for the cached file and publish the
    // pre-recorded dimensions immediately, before the bitmap even decodes. On a cache miss,
    // download the cache copy in the background so recycled realizations use the cache.
    private async Task InitializeFromCacheAsync()
    {
        var (pixelWidth, pixelHeight) = await MediaCacheService.TryGetPixelSizeAsync(InlineImageMediaId);
        if (pixelWidth > 0)
        {
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            HasPixelSize = true;

            if (ImageSource is { UriSource: not null })
            {
                var cachedImageSource = await MediaCacheService.CreateCachedImageSourceAsync(InlineImageMediaId);
                if (cachedImageSource != null) ImageSource = cachedImageSource;
            }

            WeakReferenceMessenger.Default.Send(new MediaImageSizeReportedMessage(this));
        }
        else _ = MediaCacheService.DownloadAsync(InlineImageMediaId);
    }

    // Reports the decoded image's natural pixel size so the carousel can recompute its height.
    // Ignored once the cache already supplied the dimensions, so a late ImageOpened from a
    // recycled element cannot clobber the cached size with a recycled bitmap.
    internal void ReportImageSize(int pixelWidth, int pixelHeight)
    {
        if (HasPixelSize || pixelWidth <= 0 || pixelHeight <= 0) return;
        if (PixelWidth == pixelWidth && PixelHeight == pixelHeight) return;

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        WeakReferenceMessenger.Default.Send(new MediaImageSizeReportedMessage(this));
    }

    // The inline image is served from the thumbnail for wrapped posts and videos, and from the
    // original media for unwrapped image posts (see CreateInlineImageSource). Full-screen
    // images use the original media; full-screen videos use the thumbnail, because the
    // original of a video is a video file that BitmapImage cannot decode.
    private static string GetInlineImageMediaId(MediaContent mediaContent, PostType postType, bool isFullScreen)
        => isFullScreen ? mediaContent.IsVideo ? mediaContent.ThumbnailMediaId ?? mediaContent.MediaId : mediaContent.MediaId : postType != PostType.Unwrapped || mediaContent.IsVideo ? mediaContent.ThumbnailMediaId ?? mediaContent.MediaId : mediaContent.MediaId;

    // Restores the initial overlay state when the carousel moves to another media: inline
    // playback stops and spoiler overlays are hidden again.
    internal void ResetForReuse()
    {
        IsPlaying = false;
        var playbackMediaSource = PlaybackMediaSource;
        PlaybackMediaSource = null;
        playbackMediaSource?.Dispose();
        IsOverlayVisible = IsVideo;
        IsSpoilerOverlayVisible = IsSpoiler;
    }

    [RelayCommand]
    private void HandleTap()
    {
        if (IsFullScreen) return;

        var index = AllMediaContents.FindIndex(x => x.MediaId == MediaContent.MediaId);
        new MediaWindow(new MediaWindowViewModel(AllMediaContents, PostType, IsParentPost, index)).ActivateModal(MainWindow.Instance);
    }

    // The inline media player element is x:Load-ed on IsPlaying, so stopping playback destroys
    // it. Destroying a live MediaPlayerElement synchronously from an Unloaded handler blocks the
    // XAML tick inside media-engine shutdown, which pumps messages and trips the renderer's
    // reentrancy guard (FailFast 0xC000027B, "Reentrancy was detected in this XAML
    // application"). Deferring the teardown to a low-priority dispatcher tick moves it outside
    // the in-flight render/unload pass, mirroring the official guidance to move this code to an
    // asynchronous event handler.
    // Order matters: pause first, then detach the source (stops decoding), and only later
    // dispose the MediaSource once the element is gone, as recommended MediaPlayer hygiene.
    [RelayCommand]
    private void HandleOverlayTap()
    {
        if (!IsVideo) return;
        StartPlayback();
    }

    // Starts playback of the original video, shared by the inline play overlay and the
    // full-screen viewer (the viewer never auto-plays videos).
    internal void StartPlayback()
    {
        IsOverlayVisible = false;
        IsPlaying = true;
        PlaybackMediaSource = MediaSource.CreateFromUri(new Uri(CommonUtils.GenerateMediaUri(MediaContent.MediaId)));
    }

    [RelayCommand]
    private void HandleSpoilerOverlayTap() => IsSpoilerOverlayVisible = false;

    [RelayCommand]
    private void HandleUnload()
    {
        var playbackMediaSource = PlaybackMediaSource;

        // Detach the source first so the element (if still realized) stops pulling from it.
        PlaybackMediaSource = null;
        IsPlaying = false;

        if (playbackMediaSource != null) DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Low, () => playbackMediaSource.Dispose()); // Low priority: run after the in-flight render/unload pass has fully unwound.
    }

    // Wrapped posts and inline videos display the thumbnail; unwrapped image posts display the
    // original media; full-screen videos display the thumbnail, and full-screen images display
    // the original media.
    // GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
    private BitmapImage CreateInlineImageSource(MediaContent mediaContent, PostType postType)
    {
        var inlineImageMediaId = GetInlineImageMediaId(mediaContent, postType, IsFullScreen);
        return inlineImageMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(inlineImageMediaId)));
    }
}