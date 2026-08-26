using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace History.MobileClient.Pages;

public partial class PublicPostsPage : ContentPage
{
    public static bool ShouldRefresh { get; set; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
#if IOS
    private double _lastScrollOffsetY;
#endif
    private HistoryPostViewModel _lastViewModel;
    private readonly ObservableCollection<HistoryPostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public PublicPostsPage()
	{
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
#if ANDROID
        WeakReferenceMessenger.Default.Register<TimelineVirtualizationChangedMessage>(this, OnTimelineVirtualizationChangedMessageReceived);
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
                catch (Exception exception) { Debug.WriteLine($"[PP] ScrollTo failed: {exception.Message}"); }

                await Task.Delay(100);
            }

            _viewModels.Clear();

            var postsResult = await App.ExecuteRequestAsync(new GetPublicPosts());
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(x => x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User) : (HistoryPostViewModel)new HistoryPublicPostViewModel(x));
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

            var lastViewModel = _viewModels.OfType<HistoryPostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
            var postsResult = await App.ExecuteRequestAsync(new GetPublicPosts(lastPostId));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User) : (HistoryPostViewModel)new HistoryPublicPostViewModel(x));
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

        if (_isFirstLoad || ShouldRefresh)
        {
            ShouldRefresh = false;
            _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }

#if ANDROID
        Dispatcher.Dispatch(ApplyVirtualizationSetting);
#endif

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

#if ANDROID
    private void OnTimelineVirtualizationChangedMessageReceived(object recipient, TimelineVirtualizationChangedMessage message) => ApplyVirtualizationSetting();

    private void ApplyVirtualizationSetting()
    {
        var isEnabled = Configuration.GetValue<bool?>("TimelineVirtualizationEnabled") ?? false;
        if (MainCollectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
            recyclerView.SetItemViewCacheSize(isEnabled ? 2 : 10);
    }
#endif

#if IOS
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        Debug.WriteLine($"[PubPosts] Scroll Recovery: {_lastScrollOffsetY}");
        MainCollectionView.SetScrollOffsetY(_lastScrollOffsetY, false);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);

        _lastScrollOffsetY = MainCollectionView.GetScrollOffsetY();
        Debug.WriteLine($"[PubPosts] _lastScrollOffsetY: {_lastScrollOffsetY}");
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
        var viewModel = view.BindingContext as HistoryPostViewModel;
        Debug.WriteLine($"[PubPosts] Child Added {viewModel.Post.Id == _lastViewModel?.Post.Id}");

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

    private void OnMainCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var collectionView = sender as CollectionView;
        var scrollOffsetY = collectionView.GetScrollOffsetY();
        if (scrollOffsetY > 0) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e) => MainCollectionView.SetScrollOffsetY(0, false);

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}