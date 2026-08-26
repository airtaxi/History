using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Messages;
using System.Collections.ObjectModel;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.ViewModels;

// Blazor search posts view model, ported from SearchPostsPage.xaml.cs. The native
// SearchBar lives in the page chrome; SearchAsync is invoked on the search button.
public partial class SearchPostsViewModel : ObservableObject, IBlazorFeedViewModel
{
    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private BasePostViewModel _lastViewModel;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private string _query;

    public ObservableCollection<BasePostViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsScrollToTopVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEmptyVisible { get; set; }

    // Plain event (deliberately not INPC) consumed by the Blazor feed to scroll
    // without triggering a full re-render.
    public event Action ScrollToTopRequested;

    public SearchPostsViewModel()
    {
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
        _lastViewModel = Items.LastOrDefault();
    }

    private static BasePostViewModel CreatePostViewModel(PostResponseDto post) => post.IsRepost ? new HistoryRepostViewModel(post.Id, post.ParentPost, post.User) : new HistoryPostViewModel(post, PostType.Timeline);

    public async Task SearchAsync(string query)
    {
        _query = query?.Trim();

        if (string.IsNullOrWhiteSpace(_query)) return;
        else if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (Items.Count > 0)
            {
                RequestScrollToTop();

                await Task.Delay(100);
            }

            Items.Clear();

            var postsResult = await App.ExecuteRequestAsync(new SearchPosts(_query));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(CreatePostViewModel);
                _lastViewModel = viewModels.LastOrDefault();

                foreach (var viewModel in viewModels) Items.Add(viewModel);
                IsEmptyVisible = !Items.Any();
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task LoadMoreAsync()
    {
        if (string.IsNullOrWhiteSpace(_query)) return;
        else if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.OfType<HistoryPostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
            var postsResult = await App.ExecuteRequestAsync(new SearchPosts(_query, lastPostId));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(CreatePostViewModel);
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    // The search page has no pull-to-refresh; the interface refresh re-runs the current query.
    public Task RefreshAsync() => SearchAsync(_query);

    public void OnAppearing() => _isInForeground = true;

    public void OnDisappearing() => _isInForeground = false;

    public void RequestScrollToTop()
    {
        ScrollToTopRequested?.Invoke();

        // Hide immediately so the border does not linger until the next scroll event.
        IsScrollToTopVisible = false;
    }

    public void SetScrollToTopVisible(bool isVisible)
    {
        if (IsScrollToTopVisible == isVisible) return;
        IsScrollToTopVisible = isVisible;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            IsLoading = isLoading;
            IsEnabled = !isLoading;
        });
    }
}
