using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.Pages;

public partial class FriendListPage : ContentPage
{
    private bool _isInForeground;
    private bool _sortByTime;
    private IEnumerable<FriendshipViewModel> _viewModels;

    private readonly string _userId;

#if IOS
    private readonly bool _isTabbedPage;
#endif

	public FriendListPage()
	{
        _userId = Shared.UserId;
        _sortByTime = Configuration.GetValue<bool>("FriendsListSortByTime");
#if IOS
        _isTabbedPage = true;
#endif
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    public FriendListPage(string userId) : this()
    {
        _userId = userId;
        TitleGrid.IsVisible = true;
    }

    private async Task RefreshAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(_userId));
        if (friendsResult.IsSuccess)
        {
            Shared.Friends = friendsResult.Value;

            _viewModels = friendsResult.Value.Select(x => new FriendshipViewModel(x));
            MainSearchBar.Text = string.Empty;
            EmptyLabel.IsVisible = !_viewModels.Any();
            TitleLabel.Text = $"{Shared.Friends.Count}명의 친구";
            FriendListLabel.Text = $"친구 목록 (총 {Shared.Friends.Count}명)";
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
            MainCollectionView.ItemsSource = _viewModels.OrderByDescending(x => x.CreatedAt);
        }
        else
        {
            SortFontImageSource.Glyph = Solid.ArrowUpAZ;
            SortLabel.Text = "이름순";
            MainCollectionView.ItemsSource = _viewModels.OrderBy(x => x.Nickname);
        }
        Configuration.SetValue("FriendsListSortByTime", _sortByTime);
    }

    private void OnMainSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModels == null) return;

        var searchBar = sender as SearchBar;
        var query = searchBar.Text?.ToLowerInvariant() ?? string.Empty;
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
        {
            MainCollectionView.ItemsSource = _viewModels;
            EmptyLabel.IsVisible = !_viewModels.Any();
        }
        else
        {
            var viewModels = _viewModels.Where(x => x.Nickname.Contains(query, StringComparison.InvariantCultureIgnoreCase) || x.User.Handle.Equals(query, StringComparison.InvariantCultureIgnoreCase));
            MainCollectionView.ItemsSource = viewModels;
            EmptyLabel.IsVisible = !viewModels.Any();
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        await RefreshAsync();

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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_userId != Shared.UserId) StatusBar.SetColor(Application.Current.Resources["Primary"] as Color);
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
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
        if (!_isTabbedPage)
        {
            AppleSwipeGestureHelper.ApplyToPage(this);
        }
#endif
    }
}