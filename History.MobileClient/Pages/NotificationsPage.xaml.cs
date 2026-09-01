using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.MobileClient.DataTypes;
using History.Commons.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class NotificationsPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMoreNotificationsToLoad;
    private readonly ObservableCollection<BaseNotificationViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public NotificationsPage()
	{
		InitializeComponent();

        MainCollectionView.ItemsSource = _viewModels;
        _isKakaoStoryMode = CommonShared.LastUsedKakaoStoryMode;
        UpdatePillVisuals();
        UpdatePillBadges();
        ApplyKakaoStoryFeaturesVisibility();

        WeakReferenceMessenger.Default.Register<NotificationsMessage>(this, OnNotificationsMessage);
        WeakReferenceMessenger.Default.Register<BadgeCountsChangedMessage>(this, OnBadgeCountsChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);
        WeakReferenceMessenger.Default.Register<KakaoStoryFeaturesEnabledMessage>(this, OnKakaoStoryFeaturesEnabledMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode)
            {
                if ((await KakaoStoryUtils.EnsureLoggedInAsync(this)) == false) return;

                try
                {
                    var notifications = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetNotifications());
                    // The mode can change while the notifications load (fast pill switching); discard the stale result, the pending switch reloads.
                    if (isKakaoStoryMode != _isKakaoStoryMode) return;

                    if (notifications == null)
                    {
                        await DisplayAlertAsync("오류", "카카오스토리 알림이 비어있습니다.", Constants.PromptOk);
                        return;
                    }

                    var viewModels = notifications.Select(x => (BaseNotificationViewModel)new KakaoNotificationViewModel(x)).ToList();
                    _viewModels.Clear();
                    foreach (var viewModel in viewModels) _viewModels.Add(viewModel);

                    Shared.KakaoStoryUnreadNotificationCount = notifications.Count(x => x.is_new);
                }
                catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 알림을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
            }
            else
            {
                var notifications = await App.ExecuteRequestAsync(new GetNotifications());
                // The mode can change while the notifications load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (notifications.IsSuccess)
                {
                    Shared.HistoryUnreadNotificationCount = notifications.Value.Count(x => x.IsUnread);
                    WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications.Value));
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreNotificationsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode) return; // Kakao Story notifications have no pagination.

            var lastViewModel = _viewModels.OfType<HistoryNotificationViewModel>().LastOrDefault();
            var notificationsResult = await App.ExecuteRequestAsync(new GetNotifications(lastViewModel.Notification.Id));
            // The mode can change while the notifications load (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (notificationsResult.IsSuccess)
            {
                var notifications = notificationsResult.Value;
                var viewModels = notifications.Select(x => new HistoryNotificationViewModel(x));
                _areThereNoMoreNotificationsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        await LoadMoreAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

#if IOS
        var tabBarHeight = LayoutHelper.GetTabBarHeight();
        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };

#endif
        Dispatcher.Dispatch(async () => await RefreshAsync());

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

    private void OnNotificationsMessage(object recipient, NotificationsMessage message)
    {
        if (_isKakaoStoryMode) return; // Kakao Story notifications are not tracked by the History notification message.

        var notifications = message.Value;
        if (notifications == null) return;

        Shared.HistoryUnreadNotificationCount = notifications.Count(x => x.IsUnread);

        _viewModels.Clear();
        var viewModels = notifications.Select(x => new HistoryNotificationViewModel(x));
        foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
    }

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

    private async void OnReadAllImageTapped(object sender, TappedEventArgs e)
    {
        var hasUnread = _viewModels.Any(x => x.IsUnread);
        if (!hasUnread) return;

        if (_isKakaoStoryMode)
        {
            await ReadAllKakaoStoryNotificationsAsync();
            return;
        }

        var result = await App.ExecuteRequestAsync(new ReadAllNotifications());
        if (result.IsSuccess)
        {
            Shared.HistoryUnreadNotificationCount = 0;
            WeakReferenceMessenger.Default.Send(new NotificationsReadAllMessage());

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Toast.Make("모든 알림을 읽음 처리했습니다.").Show();
        }
    }

    // Kakao Story has no read-all endpoint; fetching each post marks its notification
    // as read server-side, so the unread post notifications are fetched in parallel.
    private async Task ReadAllKakaoStoryNotificationsAsync()
    {
        var unreadViewModels = _viewModels.Where(x => x.IsUnread).ToList();
        if (unreadViewModels.Count == 0) return;

        var postIds = unreadViewModels.OfType<KakaoNotificationViewModel>().Select(x => x.GetPostId()).Where(x => x != null).ToList();
        if (postIds.Count > 0)
        {
            try
            {
                await App.ExecuteWithLoadingAsync(async () =>
                {
                    await Task.WhenAll(postIds.Select(postId => KakaoStoryApiHandler.GetPost(postId)));
                    return true;
                });
            }
            catch (Exception exception)
            {
                await DisplayAlertAsync("오류", $"알림 읽음 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
                return;
            }
        }

        // Mark every unread item as read locally; friend request notifications have no post to fetch.
        foreach (var viewModel in unreadViewModels) await viewModel.MarkAsReadAsync();
        Shared.KakaoStoryUnreadNotificationCount = 0;

        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        await Toast.Make("모든 알림을 읽음 처리했습니다.").Show();
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
        PillBadgeHelper.Apply(HistoryPillBadgeBorder, HistoryPillBadgeLabel, Shared.HistoryUnreadNotificationCount);

        // The Kakao Story badge respects the badge sum setting, mirroring the tab bar badge,
        // and stays hidden until the easter egg switch is unlocked on the settings page.
        var isKakaoStoryBadgeEnabled = (Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false) && (Configuration.GetValue<bool?>("KakaoStoryNotificationBadgeEnabled") ?? true);
        PillBadgeHelper.Apply(KakaoStoryPillBadgeBorder, KakaoStoryPillBadgeLabel, isKakaoStoryBadgeEnabled ? Shared.KakaoStoryUnreadNotificationCount : 0);
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

        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { return; }
    }
}
