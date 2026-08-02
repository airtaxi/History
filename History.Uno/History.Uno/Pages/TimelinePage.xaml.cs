using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Controls;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Uno.DataTypes;
using History.Uno.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;

namespace History.Uno.Pages;

public sealed partial class TimelinePage : Page
{
    public static bool ShouldRefresh { get; set; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private PeriodicTimer _scrollPositionTimer;
    private bool _lastScrollToTopBorderVisible;
    private PostViewModel _lastViewModel;
    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public TimelinePage()
    {
        InitializeComponent();
        MainItemsRepeater.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
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

                try { MainScrollViewer.ChangeView(0, 0, null, false); }
                catch (Exception) { }

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
            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(lastPostId, 30));
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

    private async void OnMainRefreshContainerRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        await RefreshAsync();
        deferral.Complete();
    }

    private void OnMainScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (e.IsIntermediate) return;

        // Load more when the user approaches the bottom of the list.
        var scrollableHeight = MainScrollViewer.ScrollableHeight;
        if (scrollableHeight - MainScrollViewer.VerticalOffset < 600) _ = LoadMoreAsync();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isInForeground = true;

        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = PollScrollPositionAsync(_scrollPositionTimer);

        if (_isFirstLoad || ShouldRefresh)
        {
            _isFirstLoad = false;
            ShouldRefresh = false;
            _ = RefreshAsync();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isInForeground = false;
        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = null;
    }

    private bool _isFirstLoad = true;

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            MainProgressRing.IsActive = isLoading;
            MainProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = !isLoading;
        });
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Replicate the MAUI column count (width / 700 + 1) via StaggeredLayout's adaptive column width.
        var newWidth = e.NewSize.Width;
        var span = ((int)newWidth / 700) + 1;
        var columnWidth = Math.Max(0, newWidth / span - 4);
        if (MainItemsRepeater.Layout is not StaggeredLayout staggeredLayout) return;
        if (Math.Abs(staggeredLayout.DesiredColumnWidth - columnWidth) < 1) return;

        staggeredLayout.DesiredColumnWidth = columnWidth;
        WeakReferenceMessenger.Default.Send(new SpanChangedMessage());
    }

    private async void OnTitleGridTapped(object sender, TappedRoutedEventArgs e) => await RefreshAsync();


    private async void OnSearchPostFontIconTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true; // Prevent call to OnTitleGridTapped (which refreshes the timeline) when the user taps the search icon.

        // TODO: Navigate to SearchPostsPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "게시글 검색은 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    private async void OnWritePostBorderTapped(object sender, TappedRoutedEventArgs e)
    {
        // TODO: Navigate to EditPostPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "게시글 작성은 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    private async Task PollScrollPositionAsync(PeriodicTimer timer)
    {
        while (await timer.WaitForNextTickAsync())
        {
            var scrollOffsetY = MainScrollViewer.VerticalOffset;
            var shouldShow = scrollOffsetY > 0;
            if (shouldShow != _lastScrollToTopBorderVisible)
            {
                ScrollToTopBorder.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                _lastScrollToTopBorderVisible = shouldShow;
            }
        }
    }

    private void OnScrollToTopBorderTapped(object sender, TappedRoutedEventArgs e)
    {
        MainScrollViewer.ChangeView(0, 0, null, false);

        // Hide immediately so the border does not linger until the next 1-second polling tick.
        ScrollToTopBorder.Visibility = Visibility.Collapsed;
        _lastScrollToTopBorderVisible = false;
    }
}
