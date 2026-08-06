using History.MobileClient.Enums;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoMediaContentViewModel : BaseMediaContentViewModel
{
    private readonly Medium _medium;
    private readonly List<Medium> _allMedias;

    public KakaoMediaContentViewModel(Medium medium, IEnumerable<Medium> allMedias, PostType postType)
        : base(medium.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true, false, null, postType, false)
    {
        _medium = medium;
        _allMedias = allMedias.ToList();

        var index = _allMedias.FindIndex(x => x.media_path == medium.media_path || x.url == medium.url);
        SetFullScreenMedias(Math.Max(0, index), _allMedias.Count > 1);
        SetMediaAndOverlay();
    }

    protected override List<IMediaViewModel> CreateFullScreenMedias(bool moreThanOneMedias)
    {
        return [.. _allMedias.Select(medium => CreateMedia(medium, true, moreThanOneMedias, PostType))];
    }

    protected override IMediaViewModel CreateInlineMedia() => CreateMedia(_medium, false, false, PostType);

    private static IMediaViewModel CreateMedia(Medium medium, bool isFullScreen, bool moreThanOneMedias, PostType postType)
    {
        var isVideo = medium.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true;
        // Timeline displays url2 (high-res display version); full screen uses origin_url (full original).
        var uri = isVideo
            ? (medium.url_hq ?? medium.url)
            : (isFullScreen ? (medium.origin_url ?? medium.url) : (medium.url2 ?? medium.thumbnail_url ?? medium.url));

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
}
