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

    [RelayCommand]
    public abstract Task HandleFriendshipActionAsync();
}
