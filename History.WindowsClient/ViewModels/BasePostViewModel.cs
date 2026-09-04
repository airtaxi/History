using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Base post view model shared by History and (future) Kakao Story post types.
// Holds the full UI surface used by the shared templates and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only.
// Menu flyouts are populated by the view models (PopulateMoreMenuFlyout/
// PopulateReactionMenuFlyout) so the platform templates remain static.
public abstract partial class BasePostViewModel(PostType postType, bool isParentPost, BaseViewModel baseViewModel) : BaseViewModel
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

    // Post-dependent simple properties — all set by derived types.
    [ObservableProperty]
    public partial string DiscoveryOptionGlyph { get; protected set; }

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; protected set; }

    [ObservableProperty]
    public partial bool HasInteractions { get; protected set; }

    [ObservableProperty]
    public partial BasePostViewModel ParentPost { get; protected set; }
    [ObservableProperty]
    public partial bool IsRepost { get; protected set; }
    [ObservableProperty]
    public partial bool IsShare { get; protected set; }

    [ObservableProperty]
    public partial bool HasRepostedUsers { get; protected set; }
    [ObservableProperty]
    public partial int RepostedUsersCount { get; protected set; }

    [ObservableProperty]
    public partial bool HasSharedUsers { get; protected set; }
    [ObservableProperty]
    public partial int SharedUsersCount { get; protected set; }

    [ObservableProperty]
    public partial bool HasReactions { get; protected set; }
    [ObservableProperty]
    public partial int ReactionsCount { get; protected set; }
    [ObservableProperty]
    public partial List<BaseInteractionViewModel> Interactions { get; protected set; }

    [ObservableProperty]
    public partial BaseInteractionViewModel Reaction { get; protected set; }
    [ObservableProperty]
    public partial string ReactionGlyph { get; protected set; }
    [ObservableProperty]
    public partial Brush ReactionBrush { get; protected set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; protected set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; protected set; }
    [ObservableProperty]
    public partial string TimestampText { get; protected set; }

    [ObservableProperty]
    public partial bool IsNotificationsMuted { get; protected set; }
    [ObservableProperty]
    public partial bool HasUnreadNotification { get; protected set; }

    [ObservableProperty]
    public partial bool IsSelectable { get; protected set; }
    [ObservableProperty]
    public partial bool IsSelected { get; protected set; }

    // Comment surface shared with the comment templates.
    [ObservableProperty]
    public partial ObservableCollection<BaseCommentViewModel> Comments { get; protected set; }
    [ObservableProperty]
    public partial BaseCommentViewModel LatestComment { get; protected set; }
    [ObservableProperty]
    public partial bool HasComments { get; protected set; }
    [ObservableProperty]
    public partial bool HasNoComments { get; protected set; }
    [ObservableProperty]
    public partial int CommentsCount { get; protected set; }
    [ObservableProperty]
    public partial bool HasMoreComments { get; protected set; }

    [ObservableProperty]
    public partial string RepostId { get; protected set; }
    [ObservableProperty]
    public partial string RepostedUserNickname { get; protected set; }
    [ObservableProperty]
    public partial string RepostPostfix { get; protected set; }
    [ObservableProperty]
    public partial string RepostCountPrefix { get; protected set; }

    public PostType PostType { get; } = postType;
    public bool IsParentPost { get; } = isParentPost;

    // Base view model used for dialog requests so the post view model works on
    // any page (including a future separate timeline window).
    public BaseViewModel BaseViewModel { get; } = baseViewModel;

    // Fills the "..." menu flyout with the actions available for the current user.
    public virtual void PopulateMoreMenuFlyout(MenuFlyout menuFlyout) => throw new NotSupportedException("[BasePostViewModel] PopulateMoreMenuFlyout must be overridden");

    // Fills the reaction flyout with the five reactions or a cancel entry when a reaction exists.
    public virtual void PopulateReactionMenuFlyout(MenuFlyout menuFlyout) => throw new NotSupportedException("[BasePostViewModel] PopulateReactionMenuFlyout must be overridden");

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual void HandleProfileTap() => throw new NotSupportedException("[BasePostViewModel] HandleProfileTap must be overridden");

    [RelayCommand]
    public virtual async Task HandleReactionAsync() => throw new NotSupportedException("[BasePostViewModel] HandleReactionAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleShareAsync() => throw new NotSupportedException("[BasePostViewModel] HandleShareAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleRepostAsync() => throw new NotSupportedException("[BasePostViewModel] HandleRepostAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleMuteNotificationsAsync() => throw new NotSupportedException("[BasePostViewModel] HandleMuteNotificationsAsync must be overridden");

    [RelayCommand]
    public virtual void HandleReactionTap() => throw new NotSupportedException("[BasePostViewModel] HandleReactionTap must be overridden");

    [RelayCommand]
    public virtual void HandleSharedTap() => throw new NotSupportedException("[BasePostViewModel] HandleSharedTap must be overridden");

    [RelayCommand]
    public virtual void HandleRepostTap() => throw new NotSupportedException("[BasePostViewModel] HandleRepostTap must be overridden");

    [RelayCommand]
    public virtual void HandleRepostedUserTap() => throw new NotSupportedException("[BasePostViewModel] HandleRepostedUserTap must be overridden");

    [RelayCommand]
    public virtual async Task HandleLoadMoreComments() => throw new NotSupportedException("[BasePostViewModel] HandleLoadMoreComments must be overridden");

    public virtual async Task<Result> RefreshAsync() => throw new NotSupportedException("[BasePostViewModel] RefreshAsync must be overridden");
}