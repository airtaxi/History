using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Message;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class MessagesPage : ContentPage
{
    private bool _isInForeground;
    private bool _areThereNoMoreMessagesToLoad;
    private readonly ObservableCollection<MessageViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public MessagesPage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            var receivedResult = await App.ExecuteRequestAsync(new GetReceivedMessages());
            var sentResult = await App.ExecuteRequestAsync(new GetSentMessages());
            if (receivedResult.IsSuccess && sentResult.IsSuccess)
            {
                var allMessages = receivedResult.Value
                    .Concat(sentResult.Value)
                    .OrderByDescending(m => m.CreatedAt);

                _viewModels.Clear();
                foreach (var message in allMessages)
                    _viewModels.Add(new MessageViewModel(message));
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreMessagesToLoad) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            var lastViewModel = _viewModels.LastOrDefault();
            var messagesResult = await App.ExecuteRequestAsync(new GetReceivedMessages(lastViewModel?.Id));
            if (messagesResult.IsSuccess)
            {
                var viewModels = messagesResult.Value.Select(x => new MessageViewModel(x));
                _areThereNoMoreMessagesToLoad = !viewModels.Any();
                foreach (var vm in viewModels) _viewModels.Add(vm);
            }
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
        await LoadMoreAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
        Dispatcher.Dispatch(async () => await RefreshAsync());
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
