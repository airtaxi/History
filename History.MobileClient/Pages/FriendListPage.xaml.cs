using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.Pages;

public partial class FriendListPage : ContentPage
{
    private bool _isInForeground;
    private bool _isFirstLoad;
    private bool _sortByTime;
    private List<HistoryFriendshipViewModel> _viewModels;

    private readonly string _userId;


	public FriendListPage()
	{
        _userId = Shared.UserId;
        _sortByTime = Configuration.GetValue<bool>("FriendsListSortByTime");

        InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<FriendshipChangedMessage>(this, OnFriendshipChangedMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    public FriendListPage(string userId) : this()
    {
        _userId = userId;

        _sortByTime = false;
        SortHorizontalStackLayout.IsVisible = false;
        
        TitleGrid.IsVisible = true;
    }

    private async Task RefreshAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(_userId));
        if (friendsResult.IsSuccess)
        {
            if (_userId == Shared.UserId) Shared.Friends = friendsResult.Value;

            _viewModels = [.. friendsResult.Value.Select(x => new HistoryFriendshipViewModel(x))];
            MainSearchBar.Text = string.Empty;
            EmptyLabel.IsVisible = !_viewModels.Any();
            var friendCount = Shared.Friends?.Count ?? 0;
            TitleLabel.Text = $"{friendCount}명의 친구";
            FriendListLabel.Text = $"친구 목록 (총 {friendCount}명)";
            ApplySort();
        }
        else if (_userId != Shared.UserId) await App.PopAsync();
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
        if (_userId != Shared.UserId) return; // Only the user's own friend list reacts to relationship changes.

        var data = message.Value;
        var isFriend = data.NewStatus == FriendshipStatus.Accepted;
        var existingViewModel = _viewModels?.FirstOrDefault(x => x.User.UserId == data.UserId);

        // Keep Shared.Friends in sync regardless of the target list, since it is used across the app.
        if (isFriend)
        {
            if (Shared.Friends != null && !Shared.Friends.Any(x => x.UserId == data.UserId)) Shared.Friends.Add(data.User);
        }
        else if (Shared.Friends != null) Shared.Friends.RemoveAll(x => x.UserId == data.UserId);

        if (_viewModels == null) return; // First load has not happened yet; it will fetch the latest data.

        if (isFriend && existingViewModel == null) _viewModels.Add(new HistoryFriendshipViewModel(data.User));
        else if (!isFriend && existingViewModel != null) _viewModels.RemoveAll(x => x.User.UserId == data.UserId);

        var friendCount = Shared.Friends?.Count ?? 0;
        TitleLabel.Text = $"{friendCount}명의 친구";
        FriendListLabel.Text = $"친구 목록 (총 {friendCount}명)";
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (_viewModels == null) return;

        var query = MainSearchBar.Text?.ToLowerInvariant()?.Trim() ?? string.Empty;
        IEnumerable<HistoryFriendshipViewModel> viewModels = _viewModels;

        if (!string.IsNullOrEmpty(query))
        {
            viewModels = viewModels.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase) || x.User.Handle.Equals(query, StringComparison.OrdinalIgnoreCase));
        }

        if (_sortByTime) viewModels = viewModels.OrderByDescending(x => x.CreatedAt);
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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_userId != Shared.UserId) StatusBar.SetColor(Application.Current.Resources["Primary"] as Color);
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

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        if (TitleGrid.IsVisible)
        {
            AppleSwipeGestureHelper.ApplyToPage(this);
        }
#endif
    }
}