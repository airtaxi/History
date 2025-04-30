using History.Commons;
using History.Commons.Api.Friendship;
using History.MobileClient.ViewModels;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class PendingFriendRequestsPage : ContentPage
{
	public PendingFriendRequestsPage()
	{
		InitializeComponent();
	}

    private async Task RefreshAsync()
    {
        var pendingUsersResult = await App.ExecuteRequestAsync(new GetPendingRequests());
        if (pendingUsersResult.IsSuccess)
        {
            var viewModels = pendingUsersResult.Value.Select(x => new FriendshipViewModel(x));
            MainCollectionView.ItemsSource = viewModels;
            EmptyLabel.IsVisible = pendingUsersResult.Value.Count == 0;
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
            await RefreshAsync();
            _isInitialized = true;
        }
    }
}