using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Platform;
using Nalu;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
{
    public static bool ShouldRefresh { get; set; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
#if IOS
    private double _lastScrollOffsetY;
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif
    private PostViewModel _lastViewModel;
    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

#if ANDROID
    private bool _isLoadingMore;
#endif

    public TimelinePage()
	{
        InitializeComponent();

#if ANDROID
        MainVirtualScroll.ItemsSource = _viewModels;
#else
        MainCollectionView.ItemsSource = _viewModels;
#endif

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);

        // Capture the original XAML margins before any tab bar inset is applied.
        _scrollToTopBorderBaseMargin = ScrollToTopBorder.Margin;
        _writePostBorderBaseMargin = WritePostBorder.Margin;
#endif
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
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

                try
                {
#if ANDROID
                    var firstIndex = _viewModels.IndexOf(firstViewModel);
                    MainVirtualScroll.ScrollTo(0, firstIndex, ScrollToPosition.Start, false);
#else
                    MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);
#endif
                }
                catch (Exception exception) { Debug.WriteLine($"[TL] ScrollTo failed: {exception.Message}"); }

                await Task.Delay(100);
            }

            _viewModels.Clear();

            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(null, 30));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(x => x.IsRepost ? new RepostViewModel(x.Id, x.ParentPost, x.User) : new PostViewModel(x, PostType.Timeline));
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
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

            var lastViewModel = _viewModels.OfType<PostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel is RepostViewModel repostViewModel ? repostViewModel.RepostId : lastViewModel.Post.Id;
            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts (lastPostId, 30));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => x.IsRepost ? new RepostViewModel(x.Id, x.ParentPost, x.User) : new PostViewModel(x, PostType.Timeline));
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();

#if ANDROID
        // VirtualScroll handles its own refresh indicator via IsRefreshing; nothing to set here.
#else
        (sender as RefreshView).IsRefreshing = false;
#endif
    }

    private bool _isFirstLoad = true;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

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

        Debug.WriteLine($"[TL] Scroll Recovery: {_lastScrollOffsetY}");
        MainCollectionView.SetScrollOffsetY(_lastScrollOffsetY, false);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);

        _lastScrollOffsetY = MainCollectionView.GetScrollOffsetY();
        Debug.WriteLine($"[TL] _lastScrollOffsetY: {_lastScrollOffsetY}");
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

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var newSpan = ((int)Width / 700) + 1;

#if ANDROID
        var previousSpan = MainStaggeredLayout.Span;
        if (newSpan != previousSpan)
        {
            // StaggeredVirtualScrollLayout supports runtime span changes: the platform handler
            // rebuilds the StaggeredGridLayoutManager automatically via LayoutInvalidated event.
            MainStaggeredLayout.Span = newSpan;

            // Fall back to a plain vertical linear layout when only one column is needed.
            MainVirtualScroll.ItemsLayout = newSpan == 1
                ? new VerticalVirtualScrollLayout()
                : MainStaggeredLayout;

            WeakReferenceMessenger.Default.Send(new SpanChangedMessage());
        }
#else
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;

        var previousSpan = staggeredItemsLayout?.Span ?? 1;
        if (newSpan != previousSpan)
        {
            if (newSpan == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };

            WeakReferenceMessenger.Default.Send(new SpanChangedMessage());
        }
#endif
    }

#if ANDROID
    private async void OnMainVirtualScrollScrolled(object sender, VirtualScrollScrolledEventArgs e)
    {
        // Show/hide scroll-to-top button based on vertical offset.
        if (e.ScrollY > 0) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;

        // Trigger LoadMore when the user is near the bottom.
        await TryLoadMoreWhenNearBottomAsync();
    }

    private async Task TryLoadMoreWhenNearBottomAsync()
    {
        if (_isLoadingMore || _areThereNoMorePostsToLoad || _fetchSemaphore.CurrentCount == 0) return;

        var range = MainVirtualScroll.GetVisibleItemsRange();
        if (range is null) return;

        // Trigger when the last visible item is within 5 positions of the end.
        if (range.Value.EndItemIndex >= _viewModels.Count - 5)
        {
            _isLoadingMore = true;
            try
            {
                // Use the last added viewmodel as the load-more anchor, mirroring the iOS ChildAdded path.
                if (_lastViewModel is not null)
                {
                    await LoadMoreAsync();
                    _lastViewModel = _viewModels.LastOrDefault();
                }
            }
            finally
            {
                _isLoadingMore = false;
            }
        }
    }
#else
    // iOS / MacCatalyst stub: VirtualScroll is hidden on Apple platforms, but XAML still references
    // the handler so it must exist with the correct signature.
    private void OnMainVirtualScrollScrolled(object sender, VirtualScrollScrolledEventArgs e) { }
#endif

    // iOS CollectionView event handlers. Defined on both platforms so XAML parses cleanly;
    // only wired up to the iOS CollectionView via OnPlatform visibility.
    private async void OnChildAdded(object sender, ElementEventArgs e)
    {
#if IOS
        var view = e.Element as View;
        var viewModel = view.BindingContext as PostViewModel;
        if (viewModel == null) return;

        Debug.WriteLine($"[TL] Child Added {viewModel.Post.Id == _lastViewModel?.Post.Id}");

        if (viewModel.Post.Id == _lastViewModel?.Post.Id)
        {
            _lastViewModel = null;
            await LoadMoreAsync();
        }
#endif
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e)
    {
#if IOS
        await LoadMoreAsync();
#endif
    }

    private void OnMainCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
#if IOS
        var collectionView = sender as CollectionView;
        var scrollOffsetY = collectionView.GetScrollOffsetY();
        if (scrollOffsetY > 0) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;
#endif
    }

    private async void OnTitleGridTapped(object sender, TappedEventArgs e) => await RefreshAsync();

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e) => await App.PushAsync(new EditPostPage());

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try
        {
#if ANDROID
            var firstIndex = _viewModels.IndexOf(firstViewModel);
            MainVirtualScroll.ScrollTo(0, firstIndex, ScrollToPosition.Start, false);
#else
            MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);
#endif
        }
        catch (Exception exception) { Debug.WriteLine($"[TL] ScrollTo failed: {exception.Message}"); }
    }

    private async void OnSearchPostImageTapped(object sender, TappedEventArgs e)
    {
        var page = new SearchPostsPage();
        await App.PushAsync(page);
    }
}