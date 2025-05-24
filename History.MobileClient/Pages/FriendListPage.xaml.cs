using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.Pages;

public partial class FriendListPage : ContentPage
{
    private bool _isInForeground;
    private bool _sortByTime;
    private IEnumerable<FriendshipViewModel> _viewModels;

    private readonly string _userId;

	public FriendListPage()
	{
        _userId = Shared.UserId;
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
            ApplySort();
        }
        else if (_userId != Shared.UserId) await App.PopModalAsync();
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

    private bool _isInitialized = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        if (!_isInitialized)
        {
            _isInitialized = true;
            _sortByTime = Configuration.GetValue<bool>("FriendsListSortByTime");

            await RefreshAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        if (!_isInForeground) return;

        Dispatcher.Dispatch(() =>
        {
            var isLoading = message.Value;
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private void OnSortTapped(object sender, TappedEventArgs e)
    {
        _sortByTime = !_sortByTime;
        ApplySort();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}