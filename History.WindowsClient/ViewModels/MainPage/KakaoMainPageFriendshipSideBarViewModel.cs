namespace History.WindowsClient.ViewModels.MainPage;

// The Kakao Story friendship side bar shares the tab strip with the History side bar;
// the ignored users tab has no Kakao Story equivalent and stays hidden in this mode.
public partial class KakaoMainPageFriendshipSideBarViewModel(MainPageViewModel mainPageViewModel) : BaseMainPageFriendshipSideBarViewModel(mainPageViewModel)
{
    protected override BaseMainPageFriendshipSideBarItemViewModel CreateSideBarItemViewModel(string tag) => tag switch
    {
        AllFriendsTag => new KakaoMainPageFriendshipSideBarAllFriendsItemViewModel(MainPageViewModel),
        "AddFriends" => new KakaoMainPageFriendshipSideBarAddFriendsItemViewModel(MainPageViewModel),
        "PendingFriends" => new KakaoMainPageFriendshipSideBarPendingFriendsItemViewModel(MainPageViewModel),
        "WaitingFriends" => new KakaoMainPageFriendshipSideBarWaitingFriendsItemViewModel(MainPageViewModel),
        "BlockedUsers" => new KakaoMainPageFriendshipSideBarBlockedUsersItemViewModel(MainPageViewModel),
        _ => null,
    };
}
