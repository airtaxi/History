using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

// Base comment view model shared by History and (future) Kakao Story comment types.
// Holds the full UI surface used by the shared comment template and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only.
public abstract partial class BaseCommentViewModel : BaseViewModel
{
    // User-dependent properties.
    [ObservableProperty]
    public partial string Nickname { get; protected set; }
    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }
    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }
    [ObservableProperty]
    public partial ImageSource ProfileThumbnailImageSource { get; protected set; }

    // Comment-dependent properties — all set by derived types.
    [ObservableProperty]
    public partial bool HasLikes { get; protected set; }
    [ObservableProperty]
    public partial int LikesCount { get; protected set; }
    [ObservableProperty]
    public partial bool Liked { get; protected set; }
    [ObservableProperty]
    public partial List<BaseFriendshipViewModel> LikedUsers { get; protected set; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReplyVisible))]
    public partial bool IsMyComment { get; protected set; }

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; protected set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; protected set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; protected set; }
    [ObservableProperty]
    public partial string TimestampText { get; protected set; }

    // The reply affordance is shown only on the post page (unwrapped view) and only for other users' comments.
    public bool IsReplyVisible => PostType == PostType.Unwrapped && !IsMyComment;

    public bool IsLongPressed { get; set; }

    protected readonly bool IsMyPost;
    protected readonly PostType PostType;
    protected readonly BasePostViewModel ParentViewModel;

    public BaseCommentViewModel(bool isMyPost, PostType postType, BasePostViewModel parentViewModel)
    {
        IsMyPost = isMyPost;
        PostType = postType;
        ParentViewModel = parentViewModel;
    }

    // Fills the comment "..." menu with the actions available for the current user.
    public virtual void PopulateMoreMenuFlyout(MenuFlyout menuFlyout) => throw new NotSupportedException("[BaseCommentViewModel] PopulateMoreMenuFlyout must be overridden");

    [RelayCommand]
    public virtual async Task HandleMore() => throw new NotSupportedException("[BaseCommentViewModel] HandleMore must be overridden");

    [RelayCommand]
    public virtual async Task HandleCommentLikeTapAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleCommentLikeTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual void HandleProfileTap() => throw new NotSupportedException("[BaseCommentViewModel] HandleProfileTap must be overridden");

    [RelayCommand]
    public virtual void HandleReply() => throw new NotSupportedException("[BaseCommentViewModel] HandleReplyAsync must be overridden");

    public virtual async Task HandleLikeAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleLikeAsync must be overridden");

    public virtual async Task DeleteAsync() => throw new NotSupportedException("[BaseCommentViewModel] DeleteAsync must be overridden");
}