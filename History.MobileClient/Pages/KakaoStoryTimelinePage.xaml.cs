using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.ThirdParty.StaggeredLayout;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Platform;
using System.Collections.ObjectModel;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.Pages;

public partial class KakaoStoryTimelinePage : ContentPage
{
    public static bool ShouldRefresh { get; set; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private PeriodicTimer _scrollPositionTimer;
    private bool _lastScrollToTopBorderVisible;
#if IOS
    private double _lastScrollOffsetY;
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif
    private PostData _lastPostData;
    private string _nextSince;
    private readonly ObservableCollection<PostData> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public KakaoStoryTimelinePage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
#if ANDROID
        WeakReferenceMessenger.Default.Register<TimelineVirtualizationChangedMessage>(this, OnTimelineVirtualizationChangedMessageReceived);
#endif
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);

        // Capture the original XAML margins before any tab bar inset is applied.
        _scrollToTopBorderBaseMargin = ScrollToTopBorder.Margin;
        _writePostBorderBaseMargin = WritePostBorder.Margin;
#endif
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (_viewModels.Count > 0)
            {
                var firstViewModel = _viewModels.FirstOrDefault();
                if (firstViewModel == null) return;

                try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
                catch (Exception) { }

                await Task.Delay(100);
            }

            _viewModels.Clear();
            _nextSince = null;

            // TODO: Fetch the first page via KakaoStoryApiHandler.GetFeed(null).
            // Make sure KakaoStoryApiHandler.Cookies is valid first
            // (login through KakaoStoryLoginPage, see SettingsPage).
            // Fill _viewModels from the returned feeds, store the next page cursor
            // into _nextSince, and set _lastPostData to the last added item so
            // OnChildAdded can trigger LoadMoreAsync.
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            // TODO: Fetch the next page via KakaoStoryApiHandler.GetFeed(_nextSince).
            // Append the new items to _viewModels, update _lastPostData and _nextSince,
            // and set _areThereNoMorePostsToLoad when the feed is exhausted.
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

        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = PollScrollPositionAsync(_scrollPositionTimer);

#if ANDROID
        // Apply virtualization setting once the handler is ready.
        Dispatcher.Dispatch(ApplyVirtualizationSetting);
#endif

        if (_isFirstLoad || ShouldRefresh)
        {
            _isFirstLoad = false;
            ShouldRefresh = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

#if IOS
        // Apply the tab bar height as bottom margin/padding here, once the native
        // tab bar has been laid out and CustomTabBarAppearanceTracker has captured
        // its height. Falls back to 49pt when the tab bar cannot be resolved yet.
        var tabBarHeight = LayoutHelper.GetTabBarHeight();

        ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + tabBarHeight);
        WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + tabBarHeight);

        MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = null;
    }

#if IOS
    private void OnTabBarHeightChangedMessageReceived(object recipient, TabBarHeightChangedMessage message)
    {
        MainCollectionView.Footer = new Grid { HeightRequest = message.Value };

        ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + message.Value);
        WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + message.Value);
    }
#endif

#if IOS
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        MainCollectionView.SetScrollOffsetY(_lastScrollOffsetY, false);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);

        _lastScrollOffsetY = MainCollectionView.GetScrollOffsetY();
    }

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

#if ANDROID
    private void OnTimelineVirtualizationChangedMessageReceived(object recipient, TimelineVirtualizationChangedMessage message) => ApplyVirtualizationSetting();

    private void ApplyVirtualizationSetting()
    {
        var isEnabled = Configuration.GetValue<bool?>("TimelineVirtualizationEnabled") ?? false;
        if (MainCollectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
        {
            // When virtualization is disabled, set a very large item view cache size so
            // off-screen Views are retained instead of being recycled.
            recyclerView.SetItemViewCacheSize(isEnabled ? 2 : 100);
        }
    }
#endif

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;

        var previousSpan = staggeredItemsLayout?.Span ?? 1;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            if (newSpan == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };

            WeakReferenceMessenger.Default.Send(new SpanChangedMessage());
        }
    }

    private async void OnChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        var postData = view.BindingContext as PostData;
        if (postData == null) return;

        if (postData.id == _lastPostData?.id)
        {
            _lastPostData = null;
            await LoadMoreAsync();
        }
    }

#if IOS
    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();
#else
    // Not used on Android, but required for compatibility
    private void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) { }
#endif

    private async void OnTitleGridTapped(object sender, TappedEventArgs e) => await RefreshAsync();

    private void OnWritePostBorderTapped(object sender, TappedEventArgs e)
    {
        // TODO: Open the Kakao Story post editor page.
    }

    private async Task PollScrollPositionAsync(PeriodicTimer timer)
    {
        while (await timer.WaitForNextTickAsync())
        {
            var scrollOffsetY = MainCollectionView.GetScrollOffsetY();
            var shouldShow = scrollOffsetY > 0;
            if (shouldShow != _lastScrollToTopBorderVisible)
            {
                ScrollToTopBorder.IsVisible = shouldShow;
                _lastScrollToTopBorderVisible = shouldShow;
            }
        }
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { }

        // Hide immediately so the border does not linger until the next 1-second polling tick.
        ScrollToTopBorder.IsVisible = false;
        _lastScrollToTopBorderVisible = false;
    }
}
