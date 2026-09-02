using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.Commons.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class PendingFriendRequestsPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private List<BaseFriendshipViewModel> _viewModels;
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public PendingFriendRequestsPage()
	{
		InitializeComponent();

        PillGrid.IsVisible = true;
        _isKakaoStoryMode = CommonShared.LastUsedKakaoStoryMode;
        UpdatePillVisuals();
        UpdatePillBadges();
        ApplyKakaoStoryFeaturesVisibility();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<BadgeCountsChangedMessage>(this, OnBadgeCountsChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<FriendshipChangedMessage>(this, OnFriendshipChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);
        WeakReferenceMessenger.Default.Register<KakaoStoryFeaturesEnabledMessage>(this, OnKakaoStoryFeaturesEnabledMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    private async Task RefreshAsync()
    {
        var isKakaoStoryMode = _isKakaoStoryMode;
        if (isKakaoStoryMode)
        {
            if ((await KakaoStoryUtils.EnsureLoggedInAsync(this)) == false) return;

            try
            {
                var invitations = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetInvitations());
                // The mode can change while the list loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                _viewModels = [.. invitations.Where(x => x.type == "received").Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x))];
                Shared.KakaoStoryPendingFriendRequestCount = invitations.Count(x => x.type == "received");
                UpdateList();
            }
            catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 받은 신청 목록을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var pendingUsersResult = await App.ExecuteRequestAsync(new GetPendingRequests());
            // The mode can change while the list loads (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (pendingUsersResult.IsSuccess)
            {
                _viewModels = [.. pendingUsersResult.Value.Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x))];
                Shared.HistoryPendingFriendRequestCount = pendingUsersResult.Value.Count;
                UpdateList();
            }
        }
    }

    private void UpdateList()
    {
        MainCollectionView.ItemsSource = _viewModels.ToList();
        EmptyLabel.IsVisible = _viewModels.Count == 0;
    }

    private void OnFriendshipChangedMessageReceived(object recipient, FriendshipChangedMessage message)
    {
        if (_isKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.

        var data = message.Value;
        if (_viewModels == null) return; // First load has not happened yet; it will fetch the latest data.

        var removedCount = _viewModels.RemoveAll(x => (x as HistoryFriendshipViewModel)?.User.UserId == data.UserId);
        if (removedCount > 0 && Shared.HistoryPendingFriendRequestCount > 0) Shared.HistoryPendingFriendRequestCount--;

        UpdateList();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private bool _isInitialized = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

#if IOS
        var tabBarHeight = LayoutHelper.GetTabBarHeight();
        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };

#endif
        if (CommonShared.LastUsedKakaoStoryMode != _isKakaoStoryMode)
        {
            await SwitchModeAsync(CommonShared.LastUsedKakaoStoryMode);
            if (CommonShared.LastUsedKakaoStoryMode == _isKakaoStoryMode) _isInitialized = true; // The switch already refreshed the list.
        }

        if (!_isInitialized)
        {
            _isInitialized = true;
            await RefreshAsync();
        }

#if !IOS
        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

#if IOS
    private void OnTabBarHeightChangedMessageReceived(object recipient, TabBarHeightChangedMessage message) => MainCollectionView.Footer = new Grid { HeightRequest = message.Value };
#endif

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        if (isKakaoStoryMode && ((await KakaoStoryUtils.EnsureLoggedInAsync(this)) == false)) return;

        await _switchSemaphore.WaitAsync();
        try
        {
            // Another tap may have applied this mode already while we waited.
            if (_isKakaoStoryMode == isKakaoStoryMode) return;
            _isKakaoStoryMode = isKakaoStoryMode;
            CommonShared.LastUsedKakaoStoryMode = isKakaoStoryMode;
            UpdatePillVisuals();
            await RefreshAsync();
        }
        finally { _switchSemaphore.Release(); }
    }

    private void UpdatePillVisuals()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color ?? Colors.Orange;
        var isDarkTheme = Utils.GetGlobalAppTheme() == AppTheme.Dark;
        var inactiveBackgroundColor = isDarkTheme ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xEA, 0xEA, 0xEA);
        var inactiveTextColor = isDarkTheme ? Color.FromRgb(0xAA, 0xAA, 0xAA) : Color.FromRgb(0x66, 0x66, 0x66);

        HistoryPillBorder.BackgroundColor = _isKakaoStoryMode ? inactiveBackgroundColor : primaryColor;
        HistoryPillLabel.TextColor = _isKakaoStoryMode ? inactiveTextColor : Colors.White;
        KakaoStoryPillBorder.BackgroundColor = _isKakaoStoryMode ? primaryColor : inactiveBackgroundColor;
        KakaoStoryPillLabel.TextColor = _isKakaoStoryMode ? Colors.White : inactiveTextColor;
    }

    private void UpdatePillBadges()
    {
        PillBadgeHelper.Apply(HistoryPillBadgeBorder, HistoryPillBadgeLabel, Shared.HistoryPendingFriendRequestCount);

        // The Kakao Story badge respects the badge sum setting, mirroring the tab bar badge,
        // and stays hidden until the easter egg switch is unlocked on the settings page.
        var isKakaoStoryBadgeEnabled = (Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false) && (Configuration.GetValue<bool?>("KakaoStoryFriendRequestBadgeEnabled") ?? true);
        PillBadgeHelper.Apply(KakaoStoryPillBadgeBorder, KakaoStoryPillBadgeLabel, isKakaoStoryBadgeEnabled ? Shared.KakaoStoryPendingFriendRequestCount : 0);
    }

    // Easter egg gate: the kakao story pill stays hidden until the switch is unlocked on the settings page.
    private void ApplyKakaoStoryFeaturesVisibility()
    {
        var isKakaoStoryFeaturesEnabled = Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false;
        KakaoStoryPillBorder.IsVisible = isKakaoStoryFeaturesEnabled;
        if (!isKakaoStoryFeaturesEnabled) UpdatePillBadges();
    }

    private void OnKakaoStoryFeaturesEnabledMessageReceived(object recipient, KakaoStoryFeaturesEnabledMessage message)
    {
        KakaoStoryPillBorder.IsVisible = true;
        UpdatePillBadges();
    }

    // The pollers run on background threads; the badge update must be marshalled to the main thread.
    private void OnBadgeCountsChangedMessageReceived(object recipient, BadgeCountsChangedMessage message) => Dispatcher.Dispatch(UpdatePillBadges);

    private void OnTabReselectedMessageReceived(object recipient, TabReselectedMessage message)
    {
        if (!_isInForeground) return;

        var firstViewModel = _viewModels?.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { return; }
    }
}