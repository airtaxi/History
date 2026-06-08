using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Sticker;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class StickersPage : ContentPage
{
    private bool _isInForeground;
    private bool _areThereNoMoreStickersToLoad;
    private string _currentSearchQuery;
    private readonly ObservableCollection<StickerViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public StickersPage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMoreStickersToLoad = false;
            _viewModels.Clear();

            var result = string.IsNullOrEmpty(_currentSearchQuery)
                ? await App.ExecuteRequestAsync(new GetStickers())
                : await App.ExecuteRequestAsync(new SearchStickers(_currentSearchQuery));

            if (result.IsSuccess)
            {
                foreach (var sticker in result.Value)
                {
                    _viewModels.Add(new StickerViewModel(sticker));
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        if (_areThereNoMoreStickersToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;

            var result = string.IsNullOrEmpty(_currentSearchQuery)
                ? await App.ExecuteRequestAsync(new GetStickers(lastViewModel.Id))
                : await App.ExecuteRequestAsync(new SearchStickers(_currentSearchQuery, lastViewModel.Id));

            if (result.IsSuccess)
            {
                _areThereNoMoreStickersToLoad = result.Value.Count == 0;
                foreach (var sticker in result.Value)
                {
                    _viewModels.Add(new StickerViewModel(sticker));
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        await RefreshAsync();
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

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private void OnSearchImageTapped(object sender, TappedEventArgs e)
    {
        SearchGrid.IsVisible = true;
        SearchEntry.Focus();
    }

    private async void OnCloseSearchImageTapped(object sender, TappedEventArgs e)
    {
        SearchGrid.IsVisible = false;
        SearchEntry.Text = "";
        _currentSearchQuery = null;
        await RefreshAsync();
    }

    private async void OnSearchEntryCompleted(object sender, EventArgs e)
    {
        _currentSearchQuery = SearchEntry.Text?.Trim();
        await RefreshAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();

    private async void OnStickerGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as StickerViewModel;
        if (viewModel == null) return;

        var page = new StickerDetailPage(viewModel.Sticker);
        await App.PushAsync(page);
    }

    private async void OnCreateStickerBorderTapped(object sender, TappedEventArgs e)
    {
        var page = new CreateStickerPage();
        await App.PushAsync(page);
    }
}
