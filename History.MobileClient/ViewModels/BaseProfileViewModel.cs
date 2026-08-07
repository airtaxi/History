using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace History.MobileClient.ViewModels;

// Base profile view model shared by History and Kakao Story profile types.
// Holds the full UI surface used by the shared ProfileTemplate and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only
// (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseProfileViewModel : ObservableObject
{
    // User-dependent properties.
    [ObservableProperty]
    public partial string Nickname { get; protected set; }
    [ObservableProperty]
    public partial string Description { get; protected set; }
    [ObservableProperty]
    public partial string FriendshipDescription { get; protected set; }
    [ObservableProperty]
    public partial bool IsMe { get; protected set; }
    [ObservableProperty]
    public partial bool IsNotMe { get; protected set; }
    [ObservableProperty]
    public partial bool IsFriend { get; protected set; }
    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }
    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }
    [ObservableProperty]
    public partial bool IsFavorite { get; protected set; }
    [ObservableProperty]
    public partial Color FavoriteColor { get; protected set; }
    [ObservableProperty]
    public partial string FriendButtonText { get; protected set; }
    [ObservableProperty]
    public partial IMediaViewModel BackgroundMedia { get; protected set; }
    [ObservableProperty]
    public partial IMediaViewModel ProfileMedia { get; protected set; }

    // Kakao Story-only surface (History keeps the defaults).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBlocked))]
    public partial bool IsBlocked { get; protected set; }
    public bool IsNotBlocked => !IsBlocked;

    [ObservableProperty]
    public partial string BlockedUserIdText { get; protected set; }
    [ObservableProperty]
    public partial bool IsFeedBlockAvailable { get; protected set; }
    [ObservableProperty]
    public partial string FeedBlockButtonText { get; protected set; }
    [ObservableProperty]
    public partial bool IsProfileSettingsVisible { get; protected set; } = true;

    [RelayCommand]
    public virtual async Task HandleProfileTapAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileLongPressAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileLongPressAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleBackgroundTapAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleBackgroundTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFriendshipActionAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFriendshipActionAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFavoriteAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFavoriteAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileSettingsAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileSettingsAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleBanAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleBanAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFeedBlockAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFeedBlockAsync must be overridden");

    public virtual async Task RefreshAsync() => throw new NotSupportedException("[BaseProfileViewModel] RefreshAsync must be overridden");
}
