using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.Pages;

public partial class AddFriendsPage : ContentPage
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;

	public AddFriendsPage()
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

    private long _searchSequence;

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        await MainSearchBar.HideSoftInputAsync(CancellationToken.None);
        await SearchAsync(MainSearchBar.Text);
    }

    private async Task SearchAsync(string query)
    {
        var sequence = ++_searchSequence;
        var viewModels = new List<BaseFriendshipViewModel>();

        if (_isKakaoStoryMode)
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            try
            {
                var searchResults = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SearchUsers(query));
                viewModels = [.. searchResults.search_results.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x))];
            }
            catch (Exception exception) { await DisplayAlertAsync("오류", $"카카오스토리 검색에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var results = new List<UserResponseDto>();

            // Add handle results
            var handleResult = await App.ExecuteRequestAsync(new GetUserByHandle(query), [ErrorType.NotFound, ErrorType.Forbidden]);
            if (handleResult.IsSuccess) results.Add(handleResult);

            // Add nickname results
            var nicknameResults = await App.ExecuteRequestAsync(new FindUsersByNickname(query));
            if (nicknameResults.IsSuccess) results.AddRange(nicknameResults.Value);

            // Remove myself from results
            results.RemoveAll(x => x.UserId == Shared.UserId);

            // Delete duplicated records
            results = [.. results.DistinctBy(x => x.UserId)];

            viewModels = [.. results.Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x))];
        }

        if (sequence != _searchSequence) return; // A newer search was issued; discard stale results.

        MainCollectionView.ItemsSource = viewModels;
        EmptyLabel.IsVisible = !viewModels.Any();
    }

    private async void OnFriendshipChangedMessageReceived(object recipient, FriendshipChangedMessage message)
    {
        if (_isKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.

        var query = MainSearchBar.Text;
        if (string.IsNullOrWhiteSpace(query)) return; // No search results are shown yet.

        await SearchAsync(query);
    }

    private async void OnFriendCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null) return;

        var collectionView = sender as CollectionView;
        collectionView.SelectedItem = null;

        var viewModel = e.CurrentSelection as HistoryFriendshipViewModel;
        await App.PushAsync(new UserPage(viewModel.User.UserId));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

#if IOS
        var tabBarHeight = LayoutHelper.GetTabBarHeight();
        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };

#endif
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

    private async void OnRefreshing(object sender, EventArgs e)
    {
        var query = MainSearchBar.Text;
        if (!string.IsNullOrWhiteSpace(query)) await SearchAsync(query);
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        if (isKakaoStoryMode && !await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        _isKakaoStoryMode = isKakaoStoryMode;
        UpdatePillVisuals();
        MainSearchBar.Placeholder = _isKakaoStoryMode ? "카카오스토리 ID 검색" : "친구의 닉네임 또는 핸들 검색";
        MainSearchBar.Text = string.Empty;
        MainCollectionView.ItemsSource = null;
        EmptyLabel.IsVisible = false;
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