using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class WaitingFriendRequestsPage : ContentPage
{
	public WaitingFriendRequestsPage()
	{
		InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
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

    private bool _isInitialized = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isInitialized)
        {
            _isInitialized = true;
            await RefreshAsync();
        }
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        MainActivityIndicator.IsRunning = isLoading;
        IsEnabled = !isLoading;
    }
}