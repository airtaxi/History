using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Message;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.Commons.KakaoStory;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class MessagesPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMoreMessagesToLoad;
    private readonly ObservableCollection<BaseMessageViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public MessagesPage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;
        _isKakaoStoryMode = CommonShared.LastUsedKakaoStoryMode;
        UpdatePillVisuals();
        UpdatePillBadges();
        ApplyKakaoStoryFeaturesVisibility();

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<MailData.Mail>>(this, OnKakaoMailDeletedMessageReceived);
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
                    var mails = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetMails());
                    // The mode can change while the mails load (fast pill switching); discard the stale result, the pending switch reloads.
                    if (isKakaoStoryMode != _isKakaoStoryMode) return;

                    if (mails == null)
                    {
                        await DisplayAlertAsync("오류", "카카오스토리 쪽지를 불러오지 못했습니다.", Constants.PromptOk);
                        return;
                    }

                    _viewModels.Clear();
                    foreach (var mail in mails) _viewModels.Add(new KakaoMessageViewModel(mail));
                    EmptyLabel.IsVisible = mails.Count == 0;

                    Shared.KakaoStoryUnreadMailCount = mails.Count(x => x.type == "receive" && x.read_at == null);
                }
                catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 쪽지를 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
            }
            else
            {
                var receivedResult = await App.ExecuteRequestAsync(new GetReceivedMessages());
                var sentResult = await App.ExecuteRequestAsync(new GetSentMessages());
                // The mode can change while the messages load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (receivedResult.IsSuccess && sentResult.IsSuccess)
                {
                    var allMessages = receivedResult.Value
                        .Concat(sentResult.Value)
                        .OrderByDescending(m => m.CreatedAt);

                    _viewModels.Clear();
                    foreach (var message in allMessages) _viewModels.Add(new HistoryMessageViewModel(message));

                    EmptyLabel.IsVisible = !allMessages.Any();

                    Shared.HistoryUnreadMailCount = receivedResult.Value.Count(x => x.ReadAt == null);
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreMessagesToLoad) return;
        try
        {
            await _fetchSemaphore.WaitAsync();

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode) return; // Kakao Story mails have no pagination.

            var lastViewModel = _viewModels.OfType<HistoryMessageViewModel>().LastOrDefault();
            if (lastViewModel == null)
            {
                _areThereNoMoreMessagesToLoad = true;
                return;
            }

            var messagesResult = await App.ExecuteRequestAsync(new GetReceivedMessages(lastViewModel.Id));
            // The mode can change while the messages load (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (messagesResult.IsSuccess)
            {
                var viewModels = messagesResult.Value.Select(x => new HistoryMessageViewModel(x));
                _areThereNoMoreMessagesToLoad = !viewModels.Any();
                foreach (var vm in viewModels) _viewModels.Add(vm);
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
        // Deferred so the iOS safe-area padding below is applied before any network work starts.
        Dispatcher.Dispatch(async () =>
        {
            if (CommonShared.LastUsedKakaoStoryMode != _isKakaoStoryMode)
            {
                await SwitchModeAsync(CommonShared.LastUsedKakaoStoryMode);
                if (CommonShared.LastUsedKakaoStoryMode == _isKakaoStoryMode) return; // The switch already refreshed the list.
            }

            await RefreshAsync();
        });

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

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private void OnKakaoMailDeletedMessageReceived(object recipient, ValueDeletedMessage<MailData.Mail> message)
    {
        var viewModels = _viewModels.OfType<KakaoMessageViewModel>().Where(x => x.Id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        if (_isKakaoStoryMode) EmptyLabel.IsVisible = _viewModels.Count == 0;

        // An unread received mail leaving the list lowers the badge count.
        if (message.Value.type == "receive" && message.Value.read_at == null && Shared.KakaoStoryUnreadMailCount > 0) Shared.KakaoStoryUnreadMailCount--;
    }

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
            _areThereNoMoreMessagesToLoad = false;
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
        PillBadgeHelper.Apply(HistoryPillBadgeBorder, HistoryPillBadgeLabel, Shared.HistoryUnreadMailCount);

        // The Kakao Story badge respects the badge sum setting, mirroring the tab bar badge,
        // and stays hidden until the easter egg switch is unlocked on the settings page.
        var isKakaoStoryBadgeEnabled = (Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false) && (Configuration.GetValue<bool?>("KakaoStoryMailBadgeEnabled") ?? true);
        PillBadgeHelper.Apply(KakaoStoryPillBadgeBorder, KakaoStoryPillBadgeLabel, isKakaoStoryBadgeEnabled ? Shared.KakaoStoryUnreadMailCount : 0);
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

    private void OnTabReselectedMessageReceived(object recipient, TabReselectedMessage message)
    {
        if (!_isInForeground) return;

        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { return; }
    }
}
