using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;

namespace History.MobileClient.Pages;

public partial class BlockedFriendsPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private List<BaseFriendshipViewModel> _viewModels;

    public BlockedFriendsPage()
	{
		InitializeComponent();

        PillGrid.IsVisible = true;
        UpdatePillVisuals();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<FriendshipChangedMessage>(this, OnFriendshipChangedMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    private async Task RefreshAsync()
    {
        if (_isKakaoStoryMode)
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            try
            {
                var bannedUsers = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetBannedUsers());
                _viewModels = [.. bannedUsers.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x))];
                UpdateList();
            }
            catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 차단 목록을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var pendingUsersResult = await App.ExecuteRequestAsync(new GetBlockedUsers());
            if (pendingUsersResult.IsSuccess)
            {
                _viewModels = [.. pendingUsersResult.Value.Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x))];
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

        if (data.NewStatus == FriendshipStatus.Blocked)
        {
            if (_viewModels.Any(x => (x as HistoryFriendshipViewModel)?.User.UserId == data.UserId)) return;
            _viewModels.Add(new HistoryFriendshipViewModel(data.User));
        }
        else _viewModels.RemoveAll(x => (x as HistoryFriendshipViewModel)?.User.UserId == data.UserId);

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

        if (isKakaoStoryMode && !await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        _isKakaoStoryMode = isKakaoStoryMode;
        UpdatePillVisuals();
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