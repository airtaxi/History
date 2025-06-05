using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class IgnoredFriendsPage : ContentPage
{
    private bool _isInForeground;

    public IgnoredFriendsPage()
	{
		InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private async Task RefreshAsync()
    {
        var pendingUsersResult = await App.ExecuteRequestAsync(new GetIgnoredUsers());
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
}