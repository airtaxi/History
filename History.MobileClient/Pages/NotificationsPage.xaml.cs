using Android.Gms.Common.Apis;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class NotificationsPage : ContentPage
{
    private bool _isInForeground;
    private readonly ObservableCollection<NotificationViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public NotificationsPage()
	{
		InitializeComponent();

        MainCollectionView.ItemsSource = _viewModels;
        WeakReferenceMessenger.Default.Register<NotificationsMessage>(this, OnNotificationsMessage);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    public async Task RefreshAsync()
    {
        try
        {
            if (_fetchSemaphore.CurrentCount == 0) return;

            await _fetchSemaphore.WaitAsync();

            var notifications = await App.ExecuteRequestAsync(new GetNotifications());
            if (notifications.IsSuccess) WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications.Value));
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.OfType<NotificationViewModel>().LastOrDefault();
            var notificationsResult = await App.ExecuteRequestAsync(new GetNotifications(lastViewModel.Notification.Id));
            if (notificationsResult.IsSuccess)
            {
                var notifications = notificationsResult.Value;
                var viewModels = notifications.Select(x => new NotificationViewModel(x));
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else return;
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        await LoadMoreAsync();
    }

    private bool _isInitialized = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        if (_isInitialized) return;
        _isInitialized = true;

        Dispatcher.Dispatch(async () => await RefreshAsync());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnNotificationsMessage(object recipient, NotificationsMessage message)
    {
        var notifications = message.Value;
        if (notifications == null) return;

        _viewModels.Clear();
        var viewModels = notifications.Select(x => new NotificationViewModel(x));
        foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
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