using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Helpers;
using History.WindowsClient.ViewModels.Friendship;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Profile;

public abstract partial class BaseProfileViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string Nickname { get; protected set; }

    [ObservableProperty]
    public partial string Description { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotMe))]
    [NotifyPropertyChangedFor(nameof(IsMemoButtonVisible))]
    [NotifyPropertyChangedFor(nameof(IsMessageButtonVisible))]
    [NotifyPropertyChangedFor(nameof(IsFriendsButtonVisible))]
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

    // Memo is a History-only action, so the Kakao Story profile hides the button
    // while the shared profile card template stays unchanged.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMemoButtonVisible))]
    public partial bool IsMemoVisible { get; protected set; } = true;

    public bool IsMemoButtonVisible => IsNotMe && IsMemoVisible;

    // Message is available on other users' profiles; the account-mode flows decide
    // whether the receiver may actually be messaged.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMessageButtonVisible))]
    public partial bool IsMessageVisible { get; protected set; } = true;

    public bool IsMessageButtonVisible => IsNotMe && IsMessageVisible;

    // Friend list flyout surface. The list is only offered on other users' profiles
    // and is reloaded on every flyout open, so no loaded flag is kept.
    private List<BaseFriendshipViewModel> _allFriends = [];

    public bool IsFriendsButtonVisible => IsNotMe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFriendsEmpty))]
    public partial ObservableCollection<BaseFriendshipViewModel> Friends { get; protected set; } = [];

    public bool IsFriendsEmpty => Friends.Count == 0;

    // Two-way bound to the flyout search box; filtering runs from the generated
    // change hook so no view-side event handler is needed.
    [ObservableProperty]
    public partial string FriendsQuery { get; set; }

    // Shown in place of the list when the owner does not publish the friend list
    // or the list is empty.
    [ObservableProperty]
    public partial string FriendsEmptyText { get; protected set; } = "친구가 없습니다";

    partial void OnFriendsQueryChanged(string value) => ApplyFriendsFilter();

    // Applies the current query to the loaded friends and swaps the displayed collection
    // so the repeater sees one reset instead of per-item changes.
    protected void ApplyFriendsFilter()
    {
        var query = FriendsQuery?.Trim();
        IEnumerable<BaseFriendshipViewModel> viewModels = _allFriends;

        if (!string.IsNullOrWhiteSpace(query)) viewModels = viewModels.Where(x => MatchesFriendsQuery(x, query));

        Friends = new(viewModels.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname));
    }

    // The History friend list also matches the handle; other platforms match the nickname only.
    protected virtual bool MatchesFriendsQuery(BaseFriendshipViewModel friendshipViewModel, string query) =>
        friendshipViewModel.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(friendshipViewModel.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase);

    // Replaces the loaded source list and re-applies the current query.
    protected void SetFriends(IEnumerable<BaseFriendshipViewModel> friends)
    {
        _allFriends = [.. friends];
        ApplyFriendsFilter();
    }

    // Ban button label: "차단 / 무시" for History (ban and ignore), "차단" for
    // Kakao Story (ban only).
    [ObservableProperty]
    public partial string BanButtonText { get; protected set; } = "차단 / 무시";

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
    public virtual void HandleProfileTap(string parameter) => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileTap must be overridden");

    [RelayCommand]
    public virtual void HandleBackgroundTap() => throw new NotSupportedException("[BaseProfileViewModel] HandleBackgroundTap must be overridden");

    [RelayCommand]
    public virtual async Task HandleMessageAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleMessageAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleMemoAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleMemoAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFriendsAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleFriendsAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleCopyProfileLinkAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleCopyProfileLinkAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileSettingsAsync() => throw new NotSupportedException("[BaseProfileViewModel] HandleProfileSettingsAsync must be overridden");
}
