using CommunityToolkit.Mvvm.ComponentModel;

namespace History.WindowsClient.ViewModels;

// Base page view model for post detail pages, shared by History and (future) Kakao Story posts.
// The post surface is platform-agnostic; comment composing differs per platform and lives on
// BaseCommentBoxViewModel, so derived page view models wire the platform-specific comment box.
public abstract partial class BasePostPageViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial BasePostViewModel Post { get; protected set; }

    [ObservableProperty]
    public partial BaseCommentBoxViewModel CommentBox { get; protected set; }
}