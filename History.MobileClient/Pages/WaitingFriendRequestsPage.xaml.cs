using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;

namespace History.MobileClient.Pages;

public partial class WaitingFriendRequestsPage : ContentPage
{
    private bool _isInForeground;
    private List<HistoryFriendshipViewModel> _viewModels;

    public WaitingFriendRequestsPage()
	{
		InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<FriendshipChangedMessage>(this, OnFriendshipChangedMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    private async Task RefreshAsync()
    {
        var waitingUsersResult = await App.ExecuteRequestAsync(new GetWaitingRequests());
        if (waitingUsersResult.IsSuccess)
        {
            _viewModels = [.. waitingUsersResult.Value.Select(x => new HistoryFriendshipViewModel(x))];
            UpdateList();
        }
    }

    private void UpdateList()
    {
        MainCollectionView.ItemsSource = _viewModels.ToList();
        EmptyLabel.IsVisible = _viewModels.Count == 0;
    }

    private void OnFriendshipChangedMessageReceived(object recipient, FriendshipChangedMessage message)
    {
        var data = message.Value;
        if (_viewModels == null) return; // First load has not happened yet; it will fetch the latest data.

        if (data.NewStatus == FriendshipStatus.Requested)
        {
            if (_viewModels.Any(x => x.User.UserId == data.UserId)) return;
            _viewModels.Add(new HistoryFriendshipViewModel(data.User));
        }
        else _viewModels.RemoveAll(x => x.User.UserId == data.UserId);

        UpdateList();
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

#if IOS
        var tabBarHeight = LayoutHelper.GetTabBarHeight();
        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };

#endif
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
}