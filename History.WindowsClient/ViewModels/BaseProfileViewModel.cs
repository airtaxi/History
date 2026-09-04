using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseProfileViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string Nickname { get; protected set; }

    [ObservableProperty]
    public partial string Description { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotMe))]
    public partial bool IsMe { get; protected set; }

    public bool IsNotMe => !IsMe;

    [ObservableProperty]
    public partial bool IsFriend { get; protected set; }

    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; protected set; }

    // Kakao Story-only surface (History keeps the defaults).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBlocked))]
    public partial bool IsBlocked { get; protected set; }
    public bool IsNotBlocked => !IsBlocked;

    // Friendship-dependent surface filled by derived types (used by the profile card template).
    [ObservableProperty]
    public partial string FriendButtonText { get; protected set; }

    [ObservableProperty]
    public partial string FriendshipDescription { get; protected set; }

    [ObservableProperty]
    public partial Brush FavoriteBrush { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileImageSource { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileThumbnailImageSource { get; protected set; }

    [ObservableProperty]
    public partial ImageSource BackgroundImageSource { get; protected set; }

    // Segoe Fluent Icons glyphs for the copy-profile-link button (the card template
    // binds the glyph so the feedback swap needs no view-side logic).
    protected const string LinkGlyph = "\uE71B";
    protected const string CheckMarkGlyph = "\uE73E";

    // Copy-feedback surface: derived types swap the link glyph with the checkmark
    // while the confirmation is shown, then restore it.
    [ObservableProperty]
    public partial string CopyProfileLinkGlyph { get; protected set; } = LinkGlyph;

    // Virtual command entry points; commands are declared here only (re-declaring
    // [RelayCommand] in a derived class would create duplicate command names).
    [RelayCommand]
    public virtual async Task HandleFriendshipActionAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFriendshipActionAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFavoriteAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFavoriteAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleBanAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleBanAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileTapAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleBackgroundTapAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleBackgroundTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleMemoAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleMemoAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleCopyProfileLinkAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleCopyProfileLinkAsync must be overridden");
}
