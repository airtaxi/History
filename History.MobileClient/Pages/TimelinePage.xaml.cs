using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
{
    public static bool ShouldRefreshTimeline { get; set; }

    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private PostViewModel _lastViewModel;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public TimelinePage()
	{
		InitializeComponent();
#if IOS
        MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

#endif
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.Where(x => x.Post.Id == message.Value.Id);
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            _viewModels.Clear();

            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts());
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(x => x.IsRepost ? new RepostViewModel(x.ParentPost, x.User) : new PostViewModel(x, true));
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else return;
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.OfType<PostViewModel>().LastOrDefault();
            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts (lastViewModel?.Post.Id));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => x.IsRepost ? new RepostViewModel(x.ParentPost, x.User) : new PostViewModel(x, true));
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else return;
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

        MainCollectionView.ItemsSource = _viewModels;
        if (_isFirstLoad || ShouldRefreshTimeline)
        {
            if (_isFirstLoad) _isFirstLoad = false;
            if (ShouldRefreshTimeline) ShouldRefreshTimeline = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;
        if (staggeredItemsLayout == null) return;

        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan) MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
    }

    private async void OnChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        var viewModel = view.BindingContext as PostViewModel;
        if (viewModel.Post.Id == _lastViewModel?.Post.Id)
        {
            _lastViewModel = null;
            await LoadMoreAsync();
        }
    }

    private async void OnWritePostImageTapped(object sender, TappedEventArgs e) => await App.PushModalAsync(new EditPostPage());

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        //MainActivityIndicator.IsRunning = isLoading;
        //IsEnabled = !isLoading;
    }

    private void OnLogoImageTapped(object sender, TappedEventArgs e)
    {
        var first = _viewModels.FirstOrDefault();
        if (first == null) return;

        MainCollectionView.ScrollTo(first);
    }
}