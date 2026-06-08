using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Moderation;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class ModerationRecordsPage : ContentPage
{
    private bool _isInForeground;
    private bool _areThereNoMoreRecordsToLoad;
    private readonly ObservableCollection<ModerationRecordViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public ModerationRecordsPage()
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

            _viewModels.Clear();
            _areThereNoMoreRecordsToLoad = false;

            var recordsResult = await App.ExecuteRequestAsync(new GetModerationRecords(null, 20));
            if (recordsResult.IsSuccess)
            {
                var viewModels = recordsResult.Value.Select(x => new ModerationRecordViewModel(x));
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreRecordsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;

            var recordsResult = await App.ExecuteRequestAsync(new GetModerationRecords(lastViewModel.Id, 20));
            if (recordsResult.IsSuccess)
            {
                var viewModels = recordsResult.Value.Select(x => new ModerationRecordViewModel(x));
                _areThereNoMoreRecordsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private bool _isFirstLoad = true;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        if (_isFirstLoad)
        {
            _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
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
        if (!_isInForeground && isLoading) return;

        // Since MAUI 10.0.70, Dispatcher.Dispatch and MainThread.BeginInvokeOnMainThread can hang the UI on iOS after async work.
#if ANDROID
        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
#endif
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();

    private async void OnTitleGridTapped(object sender, TappedEventArgs e) => await RefreshAsync();

    private void OnMainCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var collectionView = sender as CollectionView;
        var scrollOffsetY = collectionView.GetScrollOffsetY();
        if (scrollOffsetY > 0) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}
