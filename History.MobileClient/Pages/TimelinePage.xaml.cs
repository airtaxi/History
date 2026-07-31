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
using System.Collections.ObjectModel;
using System.Diagnostics;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
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
    private PostViewModel _lastViewModel;
    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public TimelinePage()
	{
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

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

                try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
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
        var viewModel = view.BindingContext as PostViewModel;
        if (viewModel == null) return;

        Debug.WriteLine($"[TL] Child Added {viewModel.Post.Id == _lastViewModel?.Post.Id}");

        if (viewModel.Post.Id == _lastViewModel?.Post.Id)
        {
            _lastViewModel = null;
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

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e) => await App.PushAsync(new EditPostPage());

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
        catch (Exception exception) { Debug.WriteLine($"[TL] ScrollTo failed: {exception.Message}"); }
    }

    private async void OnSearchPostImageTapped(object sender, TappedEventArgs e)
    {
        var page = new SearchPostsPage();
        await App.PushAsync(page);
    }
}