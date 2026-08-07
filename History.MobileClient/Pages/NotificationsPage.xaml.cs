using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.MobileClient.DataTypes;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.Pages;

public partial class NotificationsPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMoreNotificationsToLoad;
    private readonly ObservableCollection<BaseNotificationViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public NotificationsPage()
	{
		InitializeComponent();

        MainCollectionView.ItemsSource = _viewModels;
        UpdatePillVisuals();
        WeakReferenceMessenger.Default.Register<NotificationsMessage>(this, OnNotificationsMessage);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (_isKakaoStoryMode)
            {
                if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

                try
                {
                    var notifications = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetNotifications());
                    if (notifications == null)
                    {
                        await DisplayAlertAsync("오류", "카카오스토리 알림이 비어있습니다.", Constants.PromptOk);
                        return;
                    }

                    var viewModels = notifications.Select(x => (BaseNotificationViewModel)new KakaoNotificationViewModel(x)).ToList();
                    _viewModels.Clear();
                    foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
                }
                catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 알림을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
            }
            else
            {
                var notifications = await App.ExecuteRequestAsync(new GetNotifications());
                if (notifications.IsSuccess) WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications.Value));
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_isKakaoStoryMode) return; // Kakao Story notifications have no pagination.
        else if (_areThereNoMoreNotificationsToLoad) return;

        try
        {

            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.OfType<HistoryNotificationViewModel>().LastOrDefault();
            var notificationsResult = await App.ExecuteRequestAsync(new GetNotifications(lastViewModel.Notification.Id));
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

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
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

        var result = await App.ExecuteRequestAsync(new ReadAllNotifications());
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new NotificationsReadAllMessage());
    }

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        if (isKakaoStoryMode && !await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        _isKakaoStoryMode = isKakaoStoryMode;
        UpdatePillVisuals();
        ReadAllImage.IsVisible = !isKakaoStoryMode;
        await RefreshAsync();
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
}
