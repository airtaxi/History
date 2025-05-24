using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class PendingFriendRequestsPage : ContentPage
{
    private bool _isInForeground;

    public PendingFriendRequestsPage()
	{
		InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
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
        _isInForeground = true;

        if (!_isInitialized)
        {
            _isInitialized = true;
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
}