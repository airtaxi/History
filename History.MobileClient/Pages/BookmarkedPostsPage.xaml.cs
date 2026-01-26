using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class BookmarkedPostsPage : ContentPage
{
    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public BookmarkedPostsPage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.Where(x => x.Post.Id == message.Value.Id).ToList();
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
    }

    private async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMorePostsToLoad = false;
            _viewModels.Clear();

            var result = await App.ExecuteRequestAsync(new GetBookmarkedPosts(null, 20));
            if (result.IsSuccess)
            {
                foreach (var post in result.Value)
                {
                    if (post.IsRepost && post.ParentPost != null) _viewModels.Add(new RepostViewModel(post.Id, post.ParentPost, post.User));
                    else _viewModels.Add(new PostViewModel(post, PostType.Bookmarked));
                }

                _areThereNoMorePostsToLoad = result.Value.Count < 20;
            }

            UpdateEmptyState();
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel is RepostViewModel repostViewModel ? repostViewModel.RepostId : lastViewModel.Post.Id;
            var result = await App.ExecuteRequestAsync(new GetBookmarkedPosts(lastPostId, 20));
            if (result.IsSuccess)
            {
                foreach (var post in result.Value)
                {
                    if (post.IsRepost && post.ParentPost != null) _viewModels.Add(new RepostViewModel(post.Id, post.ParentPost, post.User));
                    else _viewModels.Add(new PostViewModel(post, PostType.Bookmarked));
                }

                _areThereNoMorePostsToLoad = result.Value.Count < 20;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private void UpdateEmptyState() => EmptyStateLayout.IsVisible = _viewModels.Count == 0;

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
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();
}
