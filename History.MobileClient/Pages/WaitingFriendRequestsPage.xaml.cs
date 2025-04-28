using History.Commons.Api.Friendship;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class WaitingFriendRequestsPage : ContentPage
{
	public WaitingFriendRequestsPage()
	{
		InitializeComponent();
    }

    private async Task RefreshAsync()
    {
        var waitingUsersResult = await App.ExecuteRequestAsync(new GetWaitingRequests());
        if (waitingUsersResult.IsSuccess)
        {
            var viewModels = waitingUsersResult.Value.Select(x => new FriendshipViewModel(x));
            MainCollectionView.ItemsSource = viewModels;
            EmptyLabel.IsVisible = waitingUsersResult.Value.Count == 0;
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

        await RefreshAsync();
    }
}