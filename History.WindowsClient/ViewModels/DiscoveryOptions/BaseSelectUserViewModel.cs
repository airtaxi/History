using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels.DiscoveryOptions;

// Base selectable user item view model for user selection dialogs.
public abstract partial class BaseSelectUserViewModel : ObservableObject
{
    public abstract string UserId { get; }

    [ObservableProperty]
    public partial string Nickname { get; protected set; }

    [ObservableProperty]
    public partial bool IsModerator { get; protected set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; protected set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileThumbnailImageSource { get; protected set; }

    public bool IsSelected
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
            SelectionChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool> SelectionChanged;

    [RelayCommand]
    public virtual void HandleTap() => IsSelected = !IsSelected;
}
