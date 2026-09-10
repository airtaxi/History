using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageViewModel : BaseViewModel
{
    private string _sideBarTag = "Friendship";

    [ObservableProperty]
    public partial bool IsKakaoStoryMode { get; private set; }

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
        else
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            var profileObject = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(CommonShared.KakaoUserId, null, true));
            MyProfileViewModel = profileObject?.profile != null ? new KakaoProfileViewModel(profileObject.profile, profileObject.mutual_friend, this) : null;

            // The badge shows the received Kakao Story friend requests.
            var invitations = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetInvitations());
            PendingFriendRequestCount = invitations?.Count(x => x.type == "received") ?? 0;
        }
    }

    // Applies the account mode requested by the mode selector. Switching to Kakao Story
    // requires a valid session; when the login is cancelled the mode stays unchanged and
    // false is returned so the view can restore the selector.
    public async Task<bool> SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (IsKakaoStoryMode == isKakaoStoryMode) return true;

        if (isKakaoStoryMode && !await KakaoStoryUtils.EnsureLoggedInAsync(this)) return false;

        IsKakaoStoryMode = isKakaoStoryMode;
        CommonShared.LastUsedKakaoStoryMode = isKakaoStoryMode;
        // Notifies the notifications flyout so its list and badge switch to the new mode.
        WeakReferenceMessenger.Default.Send(new KakaoStoryModeChangedMessage(isKakaoStoryMode));

        await RefreshAsync();

        // Rebuild the side bar for the new mode while staying on the selected tab.
        SideBarViewModel = CreateSideBarViewModel(_sideBarTag);
        await RefreshSideBarAsync();

        return true;
    }

    public async Task RefreshSideBarAsync()
    {
        if (SideBarViewModel is BaseMainPageFriendshipSideBarViewModel friendshipSideBarViewModel)
        {
            // A newly attached side bar has no content yet; it starts on the friend list.
            if (friendshipSideBarViewModel.SideBarContent is not null) await friendshipSideBarViewModel.SideBarContent.RefreshAsync();
            else await friendshipSideBarViewModel.InitializeAsync();
        }
        else if (SideBarViewModel is BaseMainPageMessagesSideBarViewModel messagesSideBarViewModel)
        {
            await messagesSideBarViewModel.RefreshAsync();
        }
    }

    public void OnSideBarSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = sender.SelectedItem?.Tag as string;
        if (tag != "Friendship" && tag != "Messages") return;

        _sideBarTag = tag;
        SideBarViewModel = CreateSideBarViewModel(tag);
        _ = RefreshSideBarAsync();
    }

    private BaseMainPageSideBarViewModel CreateSideBarViewModel(string tag)
    {
        if (tag == "Messages") return IsKakaoStoryMode ? new KakaoMainPageMessagesSideBarViewModel(this) : new HistoryMainPageMessagesSideBarViewModel(this);
        else return IsKakaoStoryMode ? new KakaoMainPageFriendshipSideBarViewModel(this) : new HistoryMainPageFriendshipSideBarViewModel(this);
    }
}
