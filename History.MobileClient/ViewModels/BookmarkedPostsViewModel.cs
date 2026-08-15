using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Enums;
using History.MobileClient.Messages;
using System.Collections.ObjectModel;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.ViewModels;

// Blazor bookmarked posts view model, ported from BookmarkedPostsPage.xaml.cs. Unlike
// the legacy XAML page, the Blazor version exposes the scroll-to-top surface (the
// original page was missing the scroll-to-top button).
public partial class BookmarkedPostsViewModel : ObservableObject, IBlazorFeedViewModel
{
    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

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

    public BookmarkedPostsViewModel()
    {
        WeakReferenceMessenger.Default.Register<PostUnbookmarkedMessage>(this, OnPostUnbookmarkedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostUnbookmarkedMessageReceived(object recipient, PostUnbookmarkedMessage message)
    {
        var viewModel = Items.OfType<HistoryPostViewModel>().FirstOrDefault(x => x.Post.Id == message.Value);
        if (viewModel == null) return;

        Items.Remove(viewModel);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMorePostsToLoad = false;
            Items.Clear();

            var result = await App.ExecuteRequestAsync(new GetBookmarkedPosts(null, 20));
            if (result.IsSuccess)
            {
                foreach (var post in result.Value)
                {
                    if (post.IsRepost && post.ParentPost != null) Items.Add(new HistoryRepostViewModel(post.Id, post.ParentPost, post.User));
                    else Items.Add(new HistoryPostViewModel(post, PostType.Bookmarked));
                }

                _areThereNoMorePostsToLoad = result.Value.Count < 20;
            }

            IsEmptyVisible = Items.Count == 0;
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.OfType<HistoryPostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
            var result = await App.ExecuteRequestAsync(new GetBookmarkedPosts(lastPostId, 20));
            if (result.IsSuccess)
            {
                foreach (var post in result.Value)
                {
                    if (post.IsRepost && post.ParentPost != null) Items.Add(new HistoryRepostViewModel(post.Id, post.ParentPost, post.User));
                    else Items.Add(new HistoryPostViewModel(post, PostType.Bookmarked));
                }

                _areThereNoMorePostsToLoad = result.Value.Count < 20;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task OnAppearingAsync()
    {
        _isInForeground = true;
        await RefreshAsync();
    }

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
