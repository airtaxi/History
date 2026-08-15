using History.MobileClient.Enums;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoMediaContentViewModel : BaseMediaContentViewModel
{
    private readonly Medium _medium;
    private readonly List<Medium> _allMedias;

    public KakaoMediaContentViewModel(Medium medium, IEnumerable<Medium> allMedias, PostType postType) : base(medium.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true, false, GetCaptionText(medium), postType, false)
    {
        _medium = medium;
        _allMedias = [.. allMedias];

        var index = _allMedias.FindIndex(x => x.media_path == medium.media_path || x.url == medium.url);
        SetFullScreenMedias(Math.Max(0, index), _allMedias.Count > 1);
        SetMediaAndOverlay();
    }

    // Kakao Story per-photo captions render as the media description overlay,
    // mirroring the History media description surface.
    private static string GetCaptionText(Medium medium)
    {
        var caption = medium.caption?.FirstOrDefault(x => x.type == "text")?.text;
        return string.IsNullOrEmpty(caption) ? null : caption;
    }

    protected override List<IMediaViewModel> CreateFullScreenMedias(bool moreThanOneMedias) => [.. _allMedias.Select(medium => CreateMedia(medium, true, moreThanOneMedias, PostType))];

    // Inline media: videos show the thumbnail (preview_url) with a play overlay,
    // matching History's behavior. The actual video (url_hq) loads on overlay tap.
    protected override IMediaViewModel CreateInlineMedia()
    {
        if (IsVideo)
        {
            var thumbnailUri = _medium.preview_url ?? _medium.preview_url_hq ?? _medium.thumbnail_url ?? _medium.url;
            return new ImageViewModel(thumbnailUri, PostType)
            {
                Aspect = PostType != PostType.Unwrapped ? Aspect.AspectFill : Aspect.AspectFit
            };
        }

        return CreateMedia(_medium, false, false, PostType);
    }

    private static IMediaViewModel CreateMedia(Medium medium, bool isFullScreen, bool moreThanOneMedias, PostType postType)
    {
        var isVideo = medium.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true;
        var uri = isVideo ? (medium.url_hq ?? medium.url) : (isFullScreen ? (medium.origin_url ?? medium.url) : (medium.url ?? medium.url2 ?? medium.thumbnail_url));

        if (isVideo)
        {
            return new VideoViewModel(uri)
            {
                Aspect = isFullScreen ? Aspect.AspectFit : Aspect.AspectFill,
                ShouldAutoPlay = true,
                ShouldLoopPlayback = true,
                ShouldMute = false,
                ShouldShowPlaybackControls = true,
                FullScreenSwipeable = moreThanOneMedias,
                IsFullScreen = isFullScreen
            };
        }

        return new ImageViewModel(uri, isFullScreen ? PostType.Unwrapped : postType)
        {
            // Match History media: full screen and detail (Unwrapped) fit the whole image, timeline crops it.
            Aspect = isFullScreen || postType == PostType.Unwrapped ? Aspect.AspectFit : Aspect.AspectFill,
            FullScreenSwipeable = moreThanOneMedias,
            IsFullScreen = isFullScreen
        };
    }

    // Overlay tap (play button): inline-play the video (url_hq) like History on Android.
    // The play overlay disappears and the video starts playing in-place.
    // Tapping the playing video then opens the full-screen viewer via HandleTapAsync.
    public override async Task HandleOverlayTap()
    {
        if (!IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

#if ANDROID
        IsOverlayVisible = false;
        Media = new VideoViewModel(_medium.url_hq ?? _medium.url)
        {
            Aspect = Aspect.AspectFill,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill
        };
#elif IOS
        // iOS does not support inline video playback in the carousel — go straight to full screen.
        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(FullScreenMedias, CurrentMedia));
        await App.PushAsync(viewerPage);
#endif
    }

}