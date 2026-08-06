using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace History.MobileClient.ViewModels;

// Base friendship view model shared by History and Kakao Story.
// Holds the surface used by the friendship template and the virtual tap entry point.
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

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseFriendshipViewModel] HandleTapAsync must be overridden");
}
