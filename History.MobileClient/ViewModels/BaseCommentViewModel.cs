using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

// Base comment view model shared by History and (future) Kakao Story comment types.
// Holds the full UI surface used by the shared comment template and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only
// (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseCommentViewModel : ObservableObject
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

    // Comment-dependent properties — all set by derived types.
    [ObservableProperty]
    public partial bool HasLikes { get; protected set; }
    [ObservableProperty]
    public partial int LikesCount { get; protected set; }

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; protected set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; protected set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; protected set; }
    [ObservableProperty]
    public partial string TimestampText { get; protected set; }

    // Raw render contents (BaseContent) for image export; the UI-level Contents
    // are indexable-compatible only (IContentViewModel), so each platform provides
    // its own raw backing data.
    public virtual List<BaseContent> GetRenderRawContents() => throw new NotSupportedException("[BaseCommentViewModel] GetRenderRawContents must be overridden");

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

    [RelayCommand]
    public virtual async Task HandleMore() => throw new NotSupportedException("[BaseCommentViewModel] HandleMore must be overridden");

    [RelayCommand]
    public virtual async Task HandleCommentLikeTapAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleCommentLikeTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileTap() => throw new NotSupportedException("[BaseCommentViewModel] HandleProfileTap must be overridden");

    public virtual async Task HandleLikeAsync() => throw new NotSupportedException("[BaseCommentViewModel] HandleLikeAsync must be overridden");

    public virtual async Task DeleteAsync() => throw new NotSupportedException("[BaseCommentViewModel] DeleteAsync must be overridden");
}
