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
    private readonly ObservableCollection<NotificationViewModel> _viewModels = [];
    private NotificationViewModel _lastViewModel;
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
                _lastViewModel = viewModels.LastOrDefault();
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

    private View _lastView;
    private void OnChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        var viewModel = view.BindingContext as NotificationViewModel;
        if (viewModel == _lastViewModel)
        {
            if (_lastView != null) _lastView.Loaded -= OnLastItemViewLoaded;
            view.Loaded += OnLastItemViewLoaded;
            _lastView = view;
        }
    }

    private async void OnLastItemViewLoaded(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount > 0)
        {
            if (_lastView != null) _lastView.Loaded -= OnLastItemViewLoaded;
            await LoadMoreAsync();
        }
    }

    private bool _isInitialized = false;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialized) return;
        _isInitialized = true;

        Dispatcher.Dispatch(async () => await RefreshAsync());
    }

    private void OnNotificationsMessage(object recipient, NotificationsMessage message)
    {
        var notifications = message.Value;
        if (notifications == null) return;

        _viewModels.Clear();
        var viewModels = notifications.Select(x => new NotificationViewModel(x));
        _lastViewModel = viewModels.LastOrDefault();
        foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        MainActivityIndicator.IsRunning = isLoading;
        IsEnabled = !isLoading;
    }
}