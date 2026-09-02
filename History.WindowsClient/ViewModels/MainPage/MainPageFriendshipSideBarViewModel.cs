using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageFriendshipSideBarViewModel(MainPageViewModel hostViewModel) : BaseMainPageSideBarViewModel
{
    public MainPageViewModel HostViewModel { get; } = hostViewModel;

    [ObservableProperty]
    public partial BaseMainPageFriendshipSideBarItemViewModel SideBarContent { get; private set; }

    // TODO: 모드 전환 시 0으로 설정 (무시한 친구 미지원 issue)
    [ObservableProperty]
    public partial int SideBarSelectedIndex { get; set; }

    public async void OnSideBarSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = sender.SelectedItem?.Tag as string;

        if (tag == "AllFriends")
        {
            var viewModel = new MainPageFriendshipSideBarAllFriendsItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
        else if (tag == "AddFriends")
        {
            var viewModel = new MainPageFriendshipSideBarAddFriendsItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
        else if (tag == "PendingFriends")
        {
            var viewModel = new MainPageFriendshipSideBarPendingFriendsItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
        else if (tag == "WaitingFriends")
        {
            var viewModel = new MainPageFriendshipSideBarWaitingFriendsItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
        else if (tag == "IgnoredUsers")
        {
            var viewModel = new MainPageFriendshipSideBarIgnoredUsersItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
        else if (tag == "BlockedUsers")
        {
            var viewModel = new MainPageFriendshipSideBarBlockedUsersItemViewModel(HostViewModel);
            SideBarContent = viewModel;
            await viewModel.RefreshAsync();
        }
    }
}
