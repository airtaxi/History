using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Core;

namespace History.WindowsClient.ViewModels.Media;

// Mirrors the MAUI BaseMediaContentViewModel + HistoryMediaContentViewModel pair for a single
// carousel item. The inline surface is image-first (thumbnail for wrapped posts and videos),
// tapping the video overlay starts inline playback, and tapping the media itself is a no-op
// until the full-screen media viewer page is implemented.
public sealed partial class MediaContentViewModel : ObservableObject
{
    public MediaContent MediaContent { get; }
    public List<MediaContent> AllMediaContents { get; }
    public PostType PostType { get; }
    public bool IsParentPost { get; }
    public bool IsVideo { get; }
    public bool IsSpoiler { get; }
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

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost)
    {
        MediaContent = mediaContent;
        AllMediaContents = allMediaContents == null ? [] : [.. allMediaContents];
        PostType = postType;
        IsParentPost = isParentPost;
        IsVideo = mediaContent.IsVideo;
        IsSpoiler = mediaContent.IsSpoiler;
        IsOverlayVisible = IsVideo;
        IsSpoilerOverlayVisible = IsSpoiler;
        Stretch = postType != PostType.Unwrapped ? Stretch.UniformToFill : Stretch.Uniform;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = Description.Length > 0;
        InlineImageMediaId = GetInlineImageMediaId(mediaContent, postType);

        // Cache hit: load from the disk cache with its pre-recorded pixel dimensions, so the
        // carousel gets its final height on the first measure pass. Cache miss: fall back to
        // the network URI now (the ImageOpened event reports the size later) and let the
        // background download cache the bitmap for the next realization.
        ImageSource = CreateInlineImageSource(mediaContent, postType);
        _ = InitializeFromCacheAsync();
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
    // original media for unwrapped image posts (see CreateInlineImageSource).
    private static string GetInlineImageMediaId(MediaContent mediaContent, PostType postType) => postType != PostType.Unwrapped || mediaContent.IsVideo ? mediaContent.ThumbnailMediaId ?? mediaContent.MediaId : mediaContent.MediaId;

    // Restores the initial overlay state when the carousel moves to another media, mirroring
    // the MAUI Unloaded command: inline playback stops and spoiler overlays are hidden again.
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
        // TODO: Open the full-screen media viewer once it is implemented.
    }

    [RelayCommand]
    private void HandleOverlayTap()
    {
        if (!IsVideo) return;

        IsOverlayVisible = false;
        IsPlaying = true;
        PlaybackMediaSource = MediaSource.CreateFromUri(new Uri(CommonUtils.GenerateMediaUri(MediaContent.MediaId)));
    }

    [RelayCommand]
    private void HandleSpoilerOverlayTap() => IsSpoilerOverlayVisible = false;

    [RelayCommand]
    private void HandleUnload()
    {
        IsPlaying = false;
        var playbackMediaSource = PlaybackMediaSource;
        PlaybackMediaSource = null;
        playbackMediaSource?.Dispose();
    }

    // Wrapped posts and inline videos display the thumbnail; unwrapped image posts display the original media.
    // GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
    private BitmapImage CreateInlineImageSource(MediaContent mediaContent, PostType postType)
    {
        var inlineImageMediaId = GetInlineImageMediaId(mediaContent, postType);
        return InlineImageMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(inlineImageMediaId)));
    }
}