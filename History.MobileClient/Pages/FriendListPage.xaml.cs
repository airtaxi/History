using History.Commons.Api.Friendship;
using History.MobileClient.ViewModels;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.Pages;

public partial class FriendListPage : ContentPage
{
    private IEnumerable<FriendshipViewModel> _viewModels;
    private bool _sortByTime = false;

	public FriendListPage()
	{
		InitializeComponent();
    }

    private async Task RefreshAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(Shared.UserId));
        if (friendsResult.IsSuccess)
        {
            _viewModels = friendsResult.Value.Select(x => new FriendshipViewModel(x));
            MainSearchBar.Text = string.Empty;
            EmptyLabel.IsVisible = !_viewModels.Any();
            ApplySort();
        }
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
            var viewModels = _viewModels.Where(x => x.Nickname.Contains(query, StringComparison.InvariantCultureIgnoreCase) || x.User.Handle.Equals(query, StringComparison.InvariantCultureIgnoreCase)).ToList();
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

        if (!_isInitialized)
        {
            _isInitialized = true;
            _sortByTime = Configuration.GetValue<bool>("FriendsListSortByTime");

            await RefreshAsync();
        }
    }

    private void OnSortTapped(object sender, TappedEventArgs e)
    {
        _sortByTime = !_sortByTime;
        ApplySort();
    }
}