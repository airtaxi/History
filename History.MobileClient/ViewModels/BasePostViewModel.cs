using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.MobileClient.Enums;
using System.Collections.ObjectModel;

namespace History.MobileClient.ViewModels;

// Base post view model shared by History and (future) Kakao Story post types.
// Holds the full UI surface used by the shared templates and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only
// (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BasePostViewModel : ObservableObject
{
    // User-dependent properties.
    [ObservableProperty]
    public partial string Nickname { get; protected set; }
    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }
    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }
    [ObservableProperty]
    public partial IMediaViewModel ProfileMedia { get; protected set; }

    // Post-dependent simple properties — all set by derived types.
    [ObservableProperty]
    public partial string DiscoveryOptionGlyph { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotWideMode))]
    public partial bool IsWideMode { get; set; }
    public bool IsNotWideMode => !IsWideMode;

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; protected set; }

    // Pre-slotted view model for timeline preview — avoids BindableLayout overhead.
    [ObservableProperty]
    public partial TimelineContentsViewModel TimelineContents { get; protected set; }

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
    public partial string ReactionFontFamily { get; protected set; }
    [ObservableProperty]
    public partial Color ReactionColor { get; protected set; }

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
    public partial DateTime CreatedAt { get; protected set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; protected set; }
    [ObservableProperty]
    public partial string TimestampText { get; protected set; }

    [ObservableProperty]
    public partial string PreviewText { get; protected set; }
    [ObservableProperty]
    public partial string PreviewTimestamp { get; protected set; }
    [ObservableProperty]
    public partial bool PreviewThumbnailVisible { get; protected set; }
    [ObservableProperty]
    public partial bool IsNotificationsMuted { get; protected set; }
    [ObservableProperty]
    public partial bool HasUnreadNotification { get; protected set; }
    [ObservableProperty]
    public partial ImageViewModel PreviewThumbnail { get; protected set; }

    // Multi-select surface used by the bulk post management page.
    [ObservableProperty]
    public partial bool IsSelectable { get; protected set; }
    [ObservableProperty]
    public partial bool IsSelected { get; protected set; }

    // Repost attribution surface shared by History and Kakao Story repost view models.
    [ObservableProperty]
    public partial string RepostId { get; protected set; }
    [ObservableProperty]
    public partial string RepostedUserNickname { get; protected set; }
    [ObservableProperty]
    public partial string RepostPostfix { get; protected set; }
    [ObservableProperty]
    public partial string RepostCountPrefix { get; protected set; }

    public PostType PostType { get; }
    public bool IsParentPost { get; }

    public BasePostViewModel(PostType postType, bool isParentPost = false)
    {
        PostType = postType;
        IsParentPost = isParentPost;
    }

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleProfileTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleMoreTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleMoreTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleReactionAsync() => throw new NotSupportedException("[BasePostViewModel] HandleReactionAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleShareAsync() => throw new NotSupportedException("[BasePostViewModel] HandleShareAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleRepostAsync() => throw new NotSupportedException("[BasePostViewModel] HandleRepostAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleMuteNotificationsAsync() => throw new NotSupportedException("[BasePostViewModel] HandleMuteNotificationsAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleReactionTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleReactionTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleSharedTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleSharedTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleRepostTapAsync() => throw new NotSupportedException("[BasePostViewModel] HandleRepostTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleLoadMoreComments() => throw new NotSupportedException("[BasePostViewModel] HandleLoadMoreComments must be overridden");

    [RelayCommand]
    public virtual async Task HandleRepostedUserTap() => throw new NotSupportedException("[BasePostViewModel] HandleRepostedUserTap must be overridden");

    public virtual async Task<Result> RefreshAsync() => throw new NotSupportedException("[BasePostViewModel] RefreshAsync must be overridden");

    public virtual async Task DisplayActionSheetAsync(bool popModal) => throw new NotSupportedException("[BasePostViewModel] DisplayActionSheetAsync must be overridden");

    public virtual async Task DeleteAsync(bool popModal) => throw new NotSupportedException("[BasePostViewModel] DeleteAsync must be overridden");
}
