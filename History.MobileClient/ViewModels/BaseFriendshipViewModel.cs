using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

// Base friendship view model shared by History and Kakao Story.
// Holds the surface used by the friendship template and the virtual command entry points.
// Commands are declared here only (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseFriendshipViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    public partial string Nickname { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    public partial bool IsModerator { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    public partial bool IsAdmin { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial IMediaViewModel ProfileMedia { get; protected set; }

    public BaseInteractionViewModel InteractionViewModel { get; protected set; }
    public bool IsInteractionAvailable => InteractionViewModel != null;

    // Friendship action surface used by the shared friendship template.
    // Kakao Story keeps the defaults (button hidden; actions live on the profile page).
    public virtual bool IsFriendshipImageVisible => false;
    public virtual string FriendshipGlyph => Solid.UserPlus;
    public virtual Color FriendshipColor => Colors.RoyalBlue;

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseFriendshipViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleFriendshipActionAsync() => throw new NotSupportedException("[BaseFriendshipViewModel] HandleFriendshipActionAsync must be overridden");
}
