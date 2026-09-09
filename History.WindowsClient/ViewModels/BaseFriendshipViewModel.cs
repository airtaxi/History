using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace History.WindowsClient.ViewModels;

// Base friendship view model shared by History and Kakao Story.
// Holds the surface used by the friendship template and the virtual command entry points.
// Commands are declared here only (adding [RelayCommand] on overrides would create duplicate command names).
public abstract partial class BaseFriendshipViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string Nickname { get; protected set; }

    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; protected set; }

    [ObservableProperty]
    public partial string FriendshipGlyph { get; protected set; }

    [ObservableProperty]
    public partial Brush FriendshipForeground { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileImageSource { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileThumbnailImageSource { get; protected set; }

    public Visibility FriendshipVisibility { get; init; } = Visibility.Visible;

    // Defaults to true; false makes the template root button transparent to input
    // so taps fall through to the hosting control (e.g. suggestion selection).
    public bool IsRootButtonHitTestVisible { get; init; } = true;

    public BaseInteractionViewModel InteractionViewModel { get; protected set; }

    public bool IsInteractionAvailable => InteractionViewModel != null;

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseFriendshipViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual void HandleProfileTap() => throw new NotSupportedException("[BaseFriendshipViewModel] HandleProfileTap must be overridden");

    [RelayCommand]
    public abstract Task HandleFriendshipActionAsync();

    public override string ToString() => Nickname ?? string.Empty;
}
