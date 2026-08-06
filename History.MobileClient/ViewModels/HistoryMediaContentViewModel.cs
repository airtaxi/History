using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class HistoryMediaContentViewModel : BaseMediaContentViewModel
{
    public MediaContent MediaContent { get; }

    private readonly List<MediaContent> _allMediaContents;

    public HistoryMediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost) : base(mediaContent.IsVideo, mediaContent.IsSpoiler, mediaContent.Description, postType, isParentPost)
    {
        MediaContent = mediaContent;
        _allMediaContents = allMediaContents.ToList();

        var index = _allMediaContents.FindIndex(x => x.MediaId == mediaContent.MediaId);
        var moreThanOneMedias = _allMediaContents.Count > 1;
#if IOS
        moreThanOneMedias = _allMediaContents.Count(x => !x.IsVideo) > 1;
#endif

        SetFullScreenMedias(index, moreThanOneMedias);
        SetMediaAndOverlay();
    }

    protected override List<IMediaViewModel> CreateFullScreenMedias(bool moreThanOneMedias) => [.. _allMediaContents.Select(mediaContent => GenerateFullScreenMedia(mediaContent, moreThanOneMedias))];

    protected override IMediaViewModel CreateInlineMedia() => new ImageViewModel(Utils.GenerateMediaUri((PostType != PostType.Unwrapped || IsVideo) ? MediaContent.ThumbnailMediaId : MediaContent.MediaId), PostType)
    {
        Aspect = PostType != PostType.Unwrapped ? Aspect.AspectFill : Aspect.AspectFit
    };

    public override async Task HandleOverlayTap()
    {
        if (!IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

#if ANDROID
        // TODO: Check for if this bug is resolved later
        if (IsParentPost && false)
        {
            var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(FullScreenMedias, CurrentMedia));
            await App.PushAsync(viewerPage);

            await CommunityToolkit.Maui.Alerts.Toast.Make("현재 공유글의 영상은 바로 재생할 수 없습니다. 전체화면 보기로 전환합니다.").Show();
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
#elif IOS
        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(FullScreenMedias, CurrentMedia));
        await App.PushAsync(viewerPage);

        await CommunityToolkit.Maui.Alerts.Toast.Make("iOS에서는 현재 인라인 영상 재생이 지원되지 않습니다. 전체화면 보기로 전환합니다.").Show();
#endif
    }

    private static IMediaViewModel GenerateFullScreenMedia(MediaContent mediaContent, bool moreThanOneMedias)
    {
        return mediaContent.IsVideo ?
        new VideoViewModel(Utils.GenerateMediaUri(mediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            ShouldAutoPlay = true,
            ShouldLoopPlayback = true,
            ShouldMute = false,
            ShouldShowPlaybackControls = true,
            FullScreenSwipeable = moreThanOneMedias,
            IsFullScreen = true
        }
        : new ImageViewModel(Utils.GenerateMediaUri(mediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            FullScreenSwipeable = moreThanOneMedias,
            IsFullScreen = true
        };
    }
}
