using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Message;
using History.Commons.Api.User;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Notifications;
using History.WindowsClient.ViewModels.Profile;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageViewModel : BaseViewModel, IRecipient<KakaoStoryFeaturesEnabledMessage>, IRecipient<LogoutRequestedMessage>
{
    private readonly SemaphoreSlim _badgeFetchSemaphore = new(1, 1);
    private string _sideBarTag = "Friendship";

    public MainPageViewModel(NotificationsViewModel notifications)
    {
        Notifications = notifications;
        KakaoStorySelectorVisibility = KakaoStoryFeatureGateHelper.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        WeakReferenceMessenger.Default.Register((IRecipient<KakaoStoryFeaturesEnabledMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<LogoutRequestedMessage>)this);
    }

    // Per-platform unread notification counts shown on the mode selector badges; the active
    // platform's count is the same value the title bar notification badge shows.
    public NotificationsViewModel Notifications { get; }

    // Revealed only after the hidden unlock, so the Kakao Story account mode is unreachable
    // while the feature set is locked.
    [ObservableProperty]
    public partial Visibility KakaoStorySelectorVisibility { get; private set; }

    [ObservableProperty]
    public partial bool IsKakaoStoryMode { get; private set; }

    [ObservableProperty]
    public partial BaseProfileViewModel MyProfileViewModel { get; private set; }

    [ObservableProperty]
    public partial BaseMainPageSideBarViewModel SideBarViewModel { get; private set; }

    [ObservableProperty]
    public partial int PendingFriendRequestCount { get; set; }

    [ObservableProperty]
    public partial int UnreadMessageCount { get; set; }

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
        }
        else
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            var profileObject = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(CommonShared.KakaoUserId, null, true));
            MyProfileViewModel = profileObject?.profile != null ? new KakaoProfileViewModel(profileObject.profile, profileObject.mutual_friend, this) : null;
        }

        // The badges stay current for the active account mode; the poller keeps them fresh
        // while the user is elsewhere in the app.
        await RefreshBadgeCountsAsync();
    }

    // Quiet badge refresh for the side bar badges: fetches only the lists that drive the unread
    // message and pending friend request counts for the active account mode, without the loading
    // overlay or error dialogs, so the badge poller can run it on a cadence.
    public async Task RefreshBadgeCountsAsync()
    {
        if (_badgeFetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _badgeFetchSemaphore.WaitAsync();

            if (CommonShared.ApiHandler == ApiHandler.Public) return;

            if (!IsKakaoStoryMode)
            {
                var messages = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetReceivedMessages());
                var pendingRequests = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetPendingRequests());

                // Signed out while the requests were in flight: the sign-out badge reset must stand.
                if (CommonShared.ApiHandler == ApiHandler.Public) return;

                UnreadMessageCount = messages?.Count(x => x.ReadAt == null) ?? 0;
                PendingFriendRequestCount = pendingRequests?.Count ?? 0;
            }
            else
            {
                if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() == null) return;

                // Background mode keeps a revoked session from popping the login modal during
                // the quiet badge refresh.
                var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
                KakaoStoryApiHandler.IsBackgroundMode = true;
                try
                {
                    var mails = await KakaoStoryApiHandler.GetMails();
                    var invitations = await KakaoStoryApiHandler.GetInvitations();

                    // Signed out while the requests were in flight: the sign-out badge reset must stand.
                    if (CommonShared.ApiHandler == ApiHandler.Public) return;

                    if (mails != null) UnreadMessageCount = mails.Count(x => x.type == "receive" && x.read_at == null);
                    if (invitations != null) PendingFriendRequestCount = invitations.Count(x => x.type == "received");
                }
                finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
            }
        }
        catch (HttpRequestException) { }
        finally { _badgeFetchSemaphore.Release(); }
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

    public void Receive(KakaoStoryFeaturesEnabledMessage message) => KakaoStorySelectorVisibility = Visibility.Visible;

    // Clears the side bar badges on sign-out so they hide immediately and the next login
    // refreshes them from the new session.
    public void Receive(LogoutRequestedMessage message)
    {
        UnreadMessageCount = 0;
        PendingFriendRequestCount = 0;
    }
}
