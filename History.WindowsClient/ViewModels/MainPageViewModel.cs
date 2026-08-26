using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.User;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

public partial class MainPageViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial bool IsKakaoStoryMode { get; set; }

    [ObservableProperty]
    public partial BaseProfileViewModel MyProfileViewModel { get; private set; }

    [ObservableProperty]
    public partial BaseMainPageSideBarViewModel SideBarViewModel { get; private set; }

    public MainPageViewModel()
    {
    }

    public async Task RefreshAsync()
    {
        if (!IsKakaoStoryMode)
        {
            var myProfileResult = await App.ExecuteRequestAsync(new GetMyProfile());
            if (!myProfileResult.IsSuccess)
            {
                await ShowMessageDialogAsync(new("오류", "프로필 정보 갱신에 실패하였습니다."));
                return;
            }

            MyProfileViewModel = new HistoryProfileViewModel(myProfileResult.Value);
        }
    }

    public void OnSideBarSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = sender.SelectedItem.Tag as string;

        if (tag == "Friendship") SideBarViewModel = new MainPageFriendshipSideBarViewModel(this);
        else if (tag == "Messages") SideBarViewModel = new MainPageMessagesSideBarViewModel(this);
    }
}
