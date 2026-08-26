using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.Commons.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using UraniumUI.Icons.FontAwesome;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.MobileClient.KakaoStory;
using History.Commons.Helpers;

namespace History.MobileClient.Pages;

public partial class FriendListPage : ContentPage
{
    private bool _isInForeground;
    private bool _isFirstLoad;
    private bool _sortByTime;
    private bool _isKakaoStoryMode;
    private List<BaseFriendshipViewModel> _viewModels;

    private readonly string _userId;
    private readonly bool _isMyProfile;
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);


	public FriendListPage()
	{
        _userId = CommonShared.UserId;
        _isMyProfile = true;
        _sortByTime = Configuration.GetValue<bool>("FriendsListSortByTime");

        InitializeComponent();

        PillGrid.IsVisible = true;
        UpdatePillVisuals();
        ApplyKakaoStoryFeaturesVisibility();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<FriendshipChangedMessage>(this, OnFriendshipChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);
        WeakReferenceMessenger.Default.Register<KakaoStoryFeaturesEnabledMessage>(this, OnKakaoStoryFeaturesEnabledMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    public FriendListPage(string userId) : this()
    {
        _userId = userId;
        _isMyProfile = false;

        _sortByTime = false;
        SortHorizontalStackLayout.IsVisible = false;
        PillGrid.IsVisible = false;
        
        TitleGrid.IsVisible = true;
    }

    public FriendListPage(string userId, bool isKakaoStoryMode) : this()
    {
        _userId = userId;
        _isKakaoStoryMode = isKakaoStoryMode;
        _isMyProfile = false;

        _sortByTime = false;
        SortHorizontalStackLayout.IsVisible = false;
        PillGrid.IsVisible = false;

        TitleGrid.IsVisible = true;
    }

    private async Task RefreshAsync()
    {
        var isKakaoStoryMode = _isKakaoStoryMode;
        if (isKakaoStoryMode)
        {
            if ((await KakaoStoryUtils.EnsureLoggedInAsync(this)) == false) return;

            try
            {
                FriendData.Friends friends;
                if (_isMyProfile) friends = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFriends());
                else friends = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFriends(_userId));
                // The mode can change while the friends load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (friends == null)
                {
                    if (!_isMyProfile)
                    {
                        await DisplayAlertAsync("안내", "친구 목록을 공개하지 않은 사용자입니다.", Constants.PromptOk);
                        await App.PopAsync();
                    }
                    return;
                }

                _viewModels = [.. friends.profiles.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x))];
                MainSearchBar.Text = string.Empty;
                EmptyLabel.IsVisible = !_viewModels.Any();
                TitleLabel.Text = $"{_viewModels.Count}명의 친구";
                FriendListLabel.Text = $"친구 목록 (총 {_viewModels.Count}명)";
                ApplyFilterAndSort();
            }
            catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 친구 목록을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var friendsResult = await App.ExecuteRequestAsync(new GetFriends(_userId));
            // The mode can change while the friends load (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (friendsResult.IsSuccess)
            {
                if (_userId == CommonShared.UserId) CommonShared.Friends = friendsResult.Value;

                _viewModels = [.. friendsResult.Value.Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x))];
                MainSearchBar.Text = string.Empty;
                EmptyLabel.IsVisible = !_viewModels.Any();
                var friendCount = CommonShared.Friends?.Count ?? 0;
                TitleLabel.Text = $"{friendCount}명의 친구";
                FriendListLabel.Text = $"친구 목록 (총 {friendCount}명)";
                ApplySort();
            }
            else if (_userId != CommonShared.UserId) await App.PopAsync();
        }
    }

    private void ApplySort()
    {
        if (_sortByTime)
        {
            SortFontImageSource.Glyph = Solid.Timeline;
            SortLabel.Text = "최신순";
        }
        else
        {
            SortFontImageSource.Glyph = Solid.ArrowUpAZ;
            SortLabel.Text = "이름순";
        }
        Configuration.SetValue("FriendsListSortByTime", _sortByTime);
        ApplyFilterAndSort();
    }

    private void OnFriendshipChangedMessageReceived(object recipient, FriendshipChangedMessage message)
    {
        if (_userId != CommonShared.UserId) return; // Only the user's own friend list reacts to relationship changes.
        if (_isKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.

        var data = message.Value;
        var isFriend = data.NewStatus == FriendshipStatus.Accepted;
        var existingViewModel = _viewModels?.OfType<HistoryFriendshipViewModel>().FirstOrDefault(x => x.User.UserId == data.UserId);

        // Keep CommonShared.Friends in sync regardless of the target list, since it is used across the app.
        if (isFriend)
        {
            if (CommonShared.Friends != null && !CommonShared.Friends.Any(x => x.UserId == data.UserId)) CommonShared.Friends.Add(data.User);
        }
        else if (CommonShared.Friends != null) CommonShared.Friends.RemoveAll(x => x.UserId == data.UserId);

        if (_viewModels == null) return; // First load has not happened yet; it will fetch the latest data.

        if (isFriend && existingViewModel == null) _viewModels.Add(new HistoryFriendshipViewModel(data.User));
        else if (!isFriend && existingViewModel != null) _viewModels.RemoveAll(x => (x as HistoryFriendshipViewModel)?.User.UserId == data.UserId);

        var friendCount = CommonShared.Friends?.Count ?? 0;
        TitleLabel.Text = $"{friendCount}명의 친구";
        FriendListLabel.Text = $"친구 목록 (총 {friendCount}명)";
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (_viewModels == null) return;

        var query = MainSearchBar.Text?.ToLowerInvariant()?.Trim() ?? string.Empty;
        IEnumerable<BaseFriendshipViewModel> viewModels = _viewModels;

        if (!string.IsNullOrEmpty(query)) viewModels = viewModels.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase)
            || (x is HistoryFriendshipViewModel historyFriendshipViewModel && historyFriendshipViewModel.User.Handle.Contains(query, StringComparison.OrdinalIgnoreCase)));

		if (_sortByTime && !_isKakaoStoryMode) viewModels = viewModels.OrderByDescending(x => (x as HistoryFriendshipViewModel)?.CreatedAt ?? DateTime.MinValue);
        else viewModels = viewModels.OrderBy(x => x.Nickname);

        MainCollectionView.ItemsSource = viewModels;
        EmptyLabel.IsVisible = !viewModels.Any();
    }

    private void OnMainSearchBarTextChanged(object sender, TextChangedEventArgs e) => ApplyFilterAndSort();

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

#if IOS
        var tabBarHeight = LayoutHelper.GetTabBarHeight();
        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };

#endif
        if (!_isFirstLoad)
        {
            _isFirstLoad = true;
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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_userId != CommonShared.UserId) StatusBar.SetColor(Application.Current.Resources["Primary"] as Color);
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

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
        return true;
    }

    private void OnSortTapped(object sender, TappedEventArgs e)
    {
        _sortByTime = !_sortByTime;
        ApplySort();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

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

    // Easter egg gate: the pill row stays hidden until the switch is unlocked on the settings page.
    private void ApplyKakaoStoryFeaturesVisibility() => PillGrid.IsVisible = Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false;
    private void OnKakaoStoryFeaturesEnabledMessageReceived(object recipient, KakaoStoryFeaturesEnabledMessage message) => PillGrid.IsVisible = true;

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        if (TitleGrid.IsVisible)
        {
            AppleSwipeGestureHelper.ApplyToPage(this);
        }
#endif
    }

    private void OnTabReselectedMessageReceived(object recipient, TabReselectedMessage message)
    {
        if (!_isInForeground) return;

        var firstViewModel = _viewModels?.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { return; }
    }
}