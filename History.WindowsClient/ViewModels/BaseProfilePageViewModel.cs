using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Shared surface for the profile pages: the profile card view model, the post feed
// with paging, and the scroll offset kept for revisit restores.
public abstract partial class BaseProfilePageViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial BaseProfileViewModel Profile { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<BasePostViewModel> Items { get; protected set; } = [];

    public bool IsEmpty => Items.Count == 0;

    // Vertical scroll offset captured continuously so revisiting the same profile
    // can restore the reading position.
    public double ScrollHeight { get; set; }

    // The user id shown on the page, used by the window to skip redundant navigation
    // to the same profile. The History and Kakao Story id spaces are separate, so each
    // platform view model returns its own id.
    public abstract string UserId { get; }

    // Stores the navigation parameter only (XamlRoot-independent, called from
    // OnNavigatedTo); the actual loading runs from OnLoaded.
    public abstract void Initialize(string userId);

    public abstract Task RefreshAsync();

    public abstract Task LoadMoreAsync();
}
