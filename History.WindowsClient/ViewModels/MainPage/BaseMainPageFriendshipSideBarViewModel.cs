using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

// Shared friendship side bar surface: the tab strip and the tab content view
// model selected for the account mode implemented by each subclass.
public abstract partial class BaseMainPageFriendshipSideBarViewModel(MainPageViewModel mainPageViewModel) : BaseMainPageSideBarViewModel
{
    protected const string AllFriendsTag = "AllFriends";

    private string _selectedTag;

    public MainPageViewModel MainPageViewModel { get; } = mainPageViewModel;

    [ObservableProperty]
    public partial BaseMainPageFriendshipSideBarItemViewModel SideBarContent { get; protected set; }

    [ObservableProperty]
    public partial int SideBarSelectedIndex { get; set; }

    // A freshly attached friendship side bar always starts on the friend list so a tab
    // selected for the previous mode or visit never carries over.
    public async Task InitializeAsync()
    {
        SideBarSelectedIndex = 0;
        await SelectTabAsync(AllFriendsTag);
    }

    public async void OnSideBarSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args) => await SelectTabAsync(sender.SelectedItem?.Tag as string);

    // Maps a tab tag to its content view model; tags the account mode does not provide return null.
    protected abstract BaseMainPageFriendshipSideBarItemViewModel CreateSideBarItemViewModel(string tag);

    private async Task SelectTabAsync(string tag)
    {
        if (tag == _selectedTag) return;

        var viewModel = CreateSideBarItemViewModel(tag);
        if (viewModel is null) return;

        _selectedTag = tag;
        SideBarContent = viewModel;
        await viewModel.RefreshAsync();
    }
}
