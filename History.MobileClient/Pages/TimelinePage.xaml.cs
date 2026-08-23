using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Platform;
using System.Collections.ObjectModel;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
{
    public static bool ShouldRefresh { get; set; }
    public static bool ShouldRefreshKakaoStory { get; set; }

    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMorePostsToLoad;
    private PeriodicTimer _scrollPositionTimer;
    private bool _lastScrollToTopBorderVisible;
#if IOS
    private double _lastScrollOffsetY;
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif
    private string _nextSince;
    private BasePostViewModel _lastViewModel;
    private readonly ObservableCollection<BasePostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public TimelinePage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnHistoryPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostData>>(this, OnKakaoPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);
        WeakReferenceMessenger.Default.Register<KakaoStoryFeaturesEnabledMessage>(this, OnKakaoStoryFeaturesEnabledMessageReceived);
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

        UpdatePillVisuals();
        ApplyKakaoStoryVisibility();
    }

    private void OnHistoryPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    private void OnKakaoPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostData> message)
    {
        var viewModels = _viewModels.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    private static string GetPostId(BasePostViewModel viewModel)
    {
        return viewModel.RepostId ?? viewModel switch
        {
            HistoryPostViewModel historyViewModel => historyViewModel.Post.Id,
            KakaoPostViewModel kakaoViewModel => kakaoViewModel.PostData.id,
            _ => null
        };
    }

    // Kakao Story bundles multiple share/UP activities into a single feed (WPF pattern);
    // the unwrapping lives in KakaoStoryUtils.CreatePostViewModel.
    private static BasePostViewModel CreateKakaoPostViewModel(PostData postData) => KakaoStoryUtils.CreatePostViewModel(postData);

    private async Task LoadFirstPageAsync()
    {
        var isKakaoStoryMode = _isKakaoStoryMode;
        if (isKakaoStoryMode)
        {
            if ((await KakaoStoryUtils.EnsureLoggedInAsync(this)) == false) return;

            var timeline = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFeed(null));
            // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (timeline?.feeds == null)
            {
                await DisplayAlertAsync("오류", "카카오스토리 피드가 비어있습니다.", Constants.PromptOk);
                return;
            }

            var viewModels = timeline.feeds.Select(CreateKakaoPostViewModel).Where(x => x != null).ToList();
            _nextSince = timeline.next_since;
            _lastViewModel = viewModels.LastOrDefault();
            foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
        }
        else
        {
            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(null, 30));
            // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User) : new HistoryPostViewModel(x, PostType.Timeline)));
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
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
            _areThereNoMorePostsToLoad = false;
            _nextSince = null;

            await LoadFirstPageAsync();
        }
        catch (Exception exception)
        {
            // History errors are surfaced by the shared request pipeline; only Kakao Story shows its own alert.
            if (_isKakaoStoryMode) await DisplayAlertAsync("오류", $"카카오스토리 피드를 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk);
            else throw;
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

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode)
            {
                var timeline = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFeed(_nextSince));
                // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (timeline?.feeds == null)
                {
                    _areThereNoMorePostsToLoad = true;
                    return;
                }

                var viewModels = timeline.feeds.Select(CreateKakaoPostViewModel).Where(x => x != null).ToList();
                _nextSince = timeline.next_since;
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = string.IsNullOrEmpty(_nextSince) || !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else
            {
                var lastViewModel = _viewModels.OfType<HistoryPostViewModel>().LastOrDefault();
                if (lastViewModel == null) return;

                var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
                var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(lastPostId, 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User) : new HistoryPostViewModel(x, PostType.Timeline)));
                    _lastViewModel = viewModels.LastOrDefault();
                    _areThereNoMorePostsToLoad = !viewModels.Any();
                    foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
                }
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

#if ANDROID
        // Apply virtualization setting once the handler is ready.
        Dispatcher.Dispatch(ApplyVirtualizationSetting);
#endif

        if (_isFirstLoad || (ShouldRefresh && !_isKakaoStoryMode) || (ShouldRefreshKakaoStory && _isKakaoStoryMode))
        {
            _isFirstLoad = false;
            ShouldRefresh = false;
            ShouldRefreshKakaoStory = false;
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
            recyclerView.SetItemViewCacheSize(isEnabled ? 2 : 10);
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
        var viewModel = view.BindingContext as BasePostViewModel;
        if (viewModel == null) return;

        if (_lastViewModel != null && GetPostId(viewModel) == GetPostId(_lastViewModel))
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

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e)
    {
        if (_isKakaoStoryMode)
        {
            var proceed = await DisplayAlertAsync("안내", KakaoStoryUtils.KakaoOnlyWriteGuideMessage, "작성", Constants.PromptCancel);
            if (!proceed) return;
            await App.PushAsync(new EditPostPage(isKakaoOnlyWrite: true));
        }
        else await App.PushAsync(new EditPostPage());
    }

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        await _switchSemaphore.WaitAsync();
        try
        {
            // Another tap may have applied this mode already while we waited.
            if (_isKakaoStoryMode == isKakaoStoryMode) return;
            _isKakaoStoryMode = isKakaoStoryMode;

            UpdatePillVisuals();

            SearchImage.IsVisible = !isKakaoStoryMode;
            ShouldRefresh = false;
            ShouldRefreshKakaoStory = false;

            await RefreshAsync();
        }
        finally { _switchSemaphore.Release(); }
    }

    private void UpdatePillVisuals()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color ?? Colors.Orange;
        var isDarkTheme = Utils.GetGlobalAppTheme() == AppTheme.Dark;
        var inactiveBackgroundColor = isDarkTheme ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xEA, 0xEA, 0xEA);
        var inactiveTextColor = isDarkTheme ? Color.FromRgb(0xAA, 0xAA, 0xAA) : Color.FromRgb(0x66, 0x66, 0x66);

        HistoryPillBorder.BackgroundColor = _isKakaoStoryMode ? inactiveBackgroundColor : primaryColor;
        HistoryPillLabel.TextColor = _isKakaoStoryMode ? inactiveTextColor : Colors.White;
        KakaoStoryPillBorder.BackgroundColor = _isKakaoStoryMode ? primaryColor : inactiveBackgroundColor;
        KakaoStoryPillLabel.TextColor = _isKakaoStoryMode ? Colors.White : inactiveTextColor;
    }

    // Easter egg gate: the kakao story pill stays hidden until the switch is unlocked on the settings page.
    private void ApplyKakaoStoryVisibility() => KakaoStoryPillBorder.IsVisible = Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false;

    private void OnKakaoStoryFeaturesEnabledMessageReceived(object recipient, KakaoStoryFeaturesEnabledMessage message)
    {
        KakaoStoryPillBorder.IsVisible = true;
        UpdatePillVisuals();
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

    private async void OnSearchPostImageTapped(object sender, TappedEventArgs e)
    {
        var page = new SearchPostsPage();
        await App.PushAsync(page);
    }

    private void OnTabReselectedMessageReceived(object recipient, TabReselectedMessage message)
    {
        if (!_isInForeground) return;

        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { return; }

        // Hide immediately so the border does not linger until the next 1-second polling tick.
        ScrollToTopBorder.IsVisible = false;
        _lastScrollToTopBorderVisible = false;
    }
}
