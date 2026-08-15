using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using System.Collections.ObjectModel;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.ViewModels;

// Blazor public posts view model, ported from PublicPostsPage.xaml.cs. The legacy
// PublicPostsPage (dead code) keeps the static ShouldRefresh flag other pages set.
public partial class PublicPostsViewModel : ObservableObject, IBlazorFeedViewModel
{
    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private bool _isFirstLoad = true;
    private BasePostViewModel _lastViewModel;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public ObservableCollection<BasePostViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsScrollToTopVisible { get; set; }

    // Plain event (deliberately not INPC) consumed by the Blazor feed to scroll
    // without triggering a full re-render.
    public event Action ScrollToTopRequested;

    public PublicPostsViewModel()
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

    private static BasePostViewModel CreatePostViewModel(PostResponseDto post) => post.IsRepost ? new HistoryRepostViewModel(post.Id, post.ParentPost, post.User) : new HistoryPublicPostViewModel(post);

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (Items.Count > 0)
            {
                RequestScrollToTop();

                await Task.Delay(100);
            }

            Items.Clear();

            var postsResult = await App.ExecuteRequestAsync(new GetPublicPosts());
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(CreatePostViewModel);
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
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
            var postsResult = await App.ExecuteRequestAsync(new GetPublicPosts(lastPostId));
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

    public async Task OnAppearingAsync()
    {
        _isInForeground = true;

        if (_isFirstLoad || PublicPostsPage.ShouldRefresh)
        {
            _isFirstLoad = false;
            PublicPostsPage.ShouldRefresh = false;
            await RefreshAsync();
        }
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
