using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.Uno.Enums;
using Microsoft.UI.Xaml.Media;

namespace History.Uno.ViewModels;

public partial class MediaContentViewModel : ObservableObject, IContentViewModel
{
    public MediaContent MediaContent { get; }
    public PostType PostType { get; }
    public bool IsParentPost { get; }
    public bool IsVideo { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsSpoiler { get; private set; }

    [ObservableProperty]
    public partial bool IsSpoilerOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial IMediaViewModel Media { get; private set; }

    public IMediaViewModel ImageMedia { get; private set; }

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost)
    {
        MediaContent = mediaContent;
        PostType = postType;
        IsParentPost = isParentPost;
        IsVideo = mediaContent.IsVideo;
        IsSpoiler = mediaContent.IsSpoiler;
        IsSpoilerOverlayVisible = IsSpoiler;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = !string.IsNullOrEmpty(Description);

        SetMediaAndOverlay();
    }

    [RelayCommand]
    public void HandleSpoilerOverlayTap() => IsSpoilerOverlayVisible = false;

    [RelayCommand]
    public async Task HandleOverlayTap()
    {
        // Play the video inline by swapping the thumbnail for a MediaPlayerElement-backed view model.
        if (!MediaContent.IsVideo) return;

        IsOverlayVisible = false;
        Media = new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId)) { Stretch = Stretch.UniformToFill };
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        // TODO: Navigate to FullScreenMediaViewerPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "전체화면 미디어 뷰어는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    private void SetMediaAndOverlay()
    {
        var mediaId = (PostType != PostType.Unwrapped || MediaContent.IsVideo) ? MediaContent.ThumbnailMediaId : MediaContent.MediaId;
        Media = new ImageViewModel(Utils.GenerateMediaUri(mediaId)) { Stretch = PostType != PostType.Unwrapped ? Stretch.UniformToFill : Stretch.Uniform };
        ImageMedia = Media;
        IsOverlayVisible = MediaContent.IsVideo;
    }
}
