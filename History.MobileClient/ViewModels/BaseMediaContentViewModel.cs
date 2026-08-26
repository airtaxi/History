using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Messages;
using History.Commons.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

// Base media content view model shared by History and (future) Kakao Story media types.
// Holds the common surface and virtual command entry points; derived types fill the
// media instances and override platform-specific behaviors. Commands are declared here
// only (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseMediaContentViewModel : ObservableObject, IContentViewModel
{
    public PostType PostType { get; }
    public bool IsParentPost { get; }
    public bool IsVideo { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; protected set; }

    [ObservableProperty]
    public partial bool IsSpoiler { get; protected set; }

    [ObservableProperty]
    public partial bool IsSpoilerOverlayVisible { get; protected set; }

    [ObservableProperty]
    public partial IMediaViewModel Media { get; protected set; }

    public IMediaViewModel ImageMedia { get; protected set; }

    protected List<IMediaViewModel> FullScreenMedias;
    protected IMediaViewModel CurrentMedia;

    public BaseMediaContentViewModel(bool isVideo, bool isSpoiler, string description, PostType postType, bool isParentPost)
    {
        IsVideo = isVideo;
        IsSpoiler = isSpoiler;
        IsSpoilerOverlayVisible = IsSpoiler;
        Description = description ?? string.Empty;
        HasDescription = !string.IsNullOrEmpty(Description);
        PostType = postType;
        IsParentPost = isParentPost;
    }

    // Factory for creating the full-screen media list. Must be overridden by derived types.
    protected virtual List<IMediaViewModel> CreateFullScreenMedias(bool moreThanOneMedias) => throw new NotSupportedException("[BaseMediaContentViewModel] CreateFullScreenMedias must be overridden");

    // Factory for creating the inline (non-full-screen) media. Must be overridden by derived types.
    protected virtual IMediaViewModel CreateInlineMedia() => throw new NotSupportedException("[BaseMediaContentViewModel] CreateInlineMedia must be overridden");

    protected void SetMediaAndOverlay()
    {
        Media = CreateInlineMedia();
        ImageMedia = Media;
        IsOverlayVisible = IsVideo;
    }

    protected void SetFullScreenMedias(int index, bool moreThanOneMedias)
    {
        FullScreenMedias = CreateFullScreenMedias(moreThanOneMedias);
        CurrentMedia = FullScreenMedias[index];
    }

    [RelayCommand]
    public void Unloaded()
    {
        if (!IsVideo) return;

        SetMediaAndOverlay();
        IsSpoilerOverlayVisible = IsSpoiler;
#if IOS
        WeakReferenceMessenger.Default.Send(new AppleVideoUnloadedMessage());
#endif
    }

    [RelayCommand]
    public void HandleSpoilerOverlayTap() => IsSpoilerOverlayVisible = false;

    // Shared behavior: opening the full-screen viewer. Derived types may override to
    // inline-play videos (e.g. History on Android).
    [RelayCommand]
    public virtual async Task HandleOverlayTap()
    {
        if (!IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(FullScreenMedias, CurrentMedia));
        await App.PushAsync(viewerPage);
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel(FullScreenMedias, CurrentMedia));
        await App.PushAsync(viewerPage);
    }
}
