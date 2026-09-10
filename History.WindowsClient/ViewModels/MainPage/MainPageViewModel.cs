using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial bool IsKakaoStoryMode { get; set; }

    [ObservableProperty]
    public partial BaseProfileViewModel MyProfileViewModel { get; private set; }

    [ObservableProperty]
    public partial BaseMainPageSideBarViewModel SideBarViewModel { get; private set; }

    [ObservableProperty]
    public partial int PendingFriendRequestCount { get; set; }

    public async Task RefreshAsync()
    {
        if (!IsKakaoStoryMode)
        {
            var myProfileResult = await ExecuteRequestAsync(new GetMyProfile());
            if (!myProfileResult.IsSuccess)
            {
                await ShowMessageDialogAsync(new(Constants.ErrorTitle, "프로필 정보 갱신에 실패하였습니다."));
                return;
            }

            MyProfileViewModel = new HistoryProfileViewModel(myProfileResult.Value, this);

            var pendingRequestResult = await ExecuteRequestAsync(new GetPendingRequests());
            if (pendingRequestResult.IsSuccess) PendingFriendRequestCount = pendingRequestResult.Value.Count;
        }
    }

    public void OnSideBarSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = sender.SelectedItem.Tag as string;

        if (tag == "Friendship") SideBarViewModel = new MainPageFriendshipSideBarViewModel(this);
        else if (tag == "Messages") SideBarViewModel = new MainPageMessagesSideBarViewModel(this);
    }
}
