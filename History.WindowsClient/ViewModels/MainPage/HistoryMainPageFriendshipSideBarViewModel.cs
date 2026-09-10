namespace History.WindowsClient.ViewModels.MainPage;

public partial class HistoryMainPageFriendshipSideBarViewModel(MainPageViewModel mainPageViewModel) : BaseMainPageFriendshipSideBarViewModel(mainPageViewModel)
{
    protected override BaseMainPageFriendshipSideBarItemViewModel CreateSideBarItemViewModel(string tag) => tag switch
    {
        AllFriendsTag => new HistoryMainPageFriendshipSideBarAllFriendsItemViewModel(MainPageViewModel),
        "AddFriends" => new HistoryMainPageFriendshipSideBarAddFriendsItemViewModel(MainPageViewModel),
        "PendingFriends" => new HistoryMainPageFriendshipSideBarPendingFriendsItemViewModel(MainPageViewModel),
        "WaitingFriends" => new HistoryMainPageFriendshipSideBarWaitingFriendsItemViewModel(MainPageViewModel),
        "IgnoredUsers" => new HistoryMainPageFriendshipSideBarIgnoredUsersItemViewModel(MainPageViewModel),
        "BlockedUsers" => new HistoryMainPageFriendshipSideBarBlockedUsersItemViewModel(MainPageViewModel),
        _ => null,
    };
}
