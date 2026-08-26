using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty]
    public partial ImageSource ProfileImageSource { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileThumbnailImageSource { get; protected set; }

    [ObservableProperty]
    public partial ImageSource BackgroundImageSource { get; protected set; }
}
