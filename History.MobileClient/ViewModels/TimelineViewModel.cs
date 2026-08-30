using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
using Application = Microsoft.Maui.Controls.Application;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.ViewModels;

// Blazor timeline view model, ported from TimelinePage.xaml.cs. Owns the feed collection
// and load/refresh/switch logic; the Blazor feed renders Items and the native chrome binds
// to the loading/scroll-to-top state. The legacy TimelinePage (dead code) keeps the static
// ShouldRefresh flags that other pages still set.
public partial class TimelineViewModel : ObservableObject, IBlazorFeedViewModel
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMorePostsToLoad;
    private bool _isFirstLoad = true;
    private string _nextSince;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public ObservableCollection<BasePostViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsScrollToTopVisible { get; set; }

    public bool IsKakaoStoryMode => _isKakaoStoryMode;

    // Easter egg switch: hides the kakao story pill until it is unlocked on the settings page.
    [ObservableProperty]
    public partial bool IsKakaoStoryFeaturesEnabled { get; private set; }

    // Plain events (deliberately not INPC) consumed by the Blazor feed and the native
    // chrome without triggering full-list re-renders.
    public event Action ScrollToTopRequested;
    public event Action<bool> ModeChanged;

    public TimelineViewModel()
    {
        IsKakaoStoryFeaturesEnabled = Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnHistoryPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostData>>(this, OnKakaoPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);
        WeakReferenceMessenger.Default.Register<KakaoStoryFeaturesEnabledMessage>(this, OnKakaoStoryFeaturesEnabledMessageReceived);
    }

    private void OnKakaoStoryFeaturesEnabledMessageReceived(object recipient, KakaoStoryFeaturesEnabledMessage message) => IsKakaoStoryFeaturesEnabled = true;

    private void OnHistoryPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
    }

    private void OnKakaoPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostData> message)
    {
        var viewModels = Items.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
    }

    // Kakao Story bundles multiple share/UP activities into a single feed (WPF pattern);
    // the unwrapping lives in KakaoStoryUtils.CreatePostViewModel.
    private static BasePostViewModel CreateKakaoPostViewModel(PostData postData) => KakaoStoryUtils.CreatePostViewModel(postData);

    private async Task LoadFirstPageAsync()
    {
        var isKakaoStoryMode = _isKakaoStoryMode;
        if (isKakaoStoryMode)
        {
            if ((await KakaoStoryUtils.EnsureLoggedInAsync(App.TopPage)) == false) return;

            var timeline = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFeed(null));
            // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != _isKakaoStoryMode) return;

            if (timeline?.feeds == null)
            {
                await App.TopPage.DisplayAlertAsync("오류", "카카오스토리 피드가 비어있습니다.", Constants.PromptOk);
                return;
            }

            var viewModels = timeline.feeds.Select(CreateKakaoPostViewModel).Where(x => x != null).ToList();
            _nextSince = timeline.next_since;
            foreach (var viewModel in viewModels) Items.Add(viewModel);
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
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
        }
    }

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
            _areThereNoMorePostsToLoad = false;
            _nextSince = null;

            await LoadFirstPageAsync();
        }
        catch (Exception exception)
        {
            // History errors are surfaced by the shared request pipeline; only Kakao Story shows its own alert.
            if (_isKakaoStoryMode) await App.TopPage.DisplayAlertAsync("오류", $"카카오스토리 피드를 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk);
            else throw;
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;
        else if (!_isInForeground) return;

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
                _areThereNoMorePostsToLoad = string.IsNullOrEmpty(_nextSince) || !viewModels.Any();
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
            else
            {
                var lastViewModel = Items.OfType<HistoryPostViewModel>().LastOrDefault();
                if (lastViewModel == null) return;

                var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
                var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(lastPostId, 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User) : new HistoryPostViewModel(x, PostType.Timeline)));
                    _areThereNoMorePostsToLoad = !viewModels.Any();
                    foreach (var viewModel in viewModels) Items.Add(viewModel);
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task OnAppearingAsync()
    {
        _isInForeground = true;

        if (_isFirstLoad || (TimelinePage.ShouldRefresh && !_isKakaoStoryMode) || (TimelinePage.ShouldRefreshKakaoStory && _isKakaoStoryMode))
        {
            _isFirstLoad = false;
            TimelinePage.ShouldRefresh = false;
            TimelinePage.ShouldRefreshKakaoStory = false;
            await RefreshAsync();
        }
    }

    public void OnDisappearing() => _isInForeground = false;

    public async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        await _switchSemaphore.WaitAsync();
        try
        {
            // Another tap may have applied this mode already while we waited.
            if (_isKakaoStoryMode == isKakaoStoryMode) return;
            _isKakaoStoryMode = isKakaoStoryMode;

            ModeChanged?.Invoke(isKakaoStoryMode);

            TimelinePage.ShouldRefresh = false;
            TimelinePage.ShouldRefreshKakaoStory = false;

            await RefreshAsync();
        }
        finally { _switchSemaphore.Release(); }
    }

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

    private void OnTabReselectedMessageReceived(object recipient, TabReselectedMessage message)
    {
        if (!_isInForeground) return;
        if (Items.Count == 0) return;

        RequestScrollToTop();
    }

    [RelayCommand]
    public async Task WritePostAsync()
    {
        if (_isKakaoStoryMode)
        {
            await App.PushAsync(new EditPostPage(isKakaoOnlyWrite: true));
        }
        else await App.PushAsync(new EditPostPage());
    }

    [RelayCommand]
    public Task SearchAsync() => App.PushAsync(new BlazorSearchPostsPage());
}
