using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
{
    private ObservableCollection<object> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public TimelinePage()
	{
		InitializeComponent();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            _viewModels.Clear();

            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts());
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var postViewModels = posts.Select(x => new PostViewModel(x, true));
                foreach (var postViewModel in postViewModels) _viewModels.Add(postViewModel);
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

            var lastPost = _viewModels.OfType<PostViewModel>().LastOrDefault();
            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(lastPost?.Post.Id));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var postViewModels = posts.Select(x => new PostViewModel(x, true));
                foreach (var postViewModel in postViewModels) _viewModels.Add(postViewModel);
            }
            else return;
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnCreateNewPostImageButtonClicked(object sender, EventArgs e)
    {
		await Application.Current.Windows[0].Page.Navigation.PushModalAsync(new EditPostPage());
    }

    private async void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;
        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
            await RefreshAsync();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MainCollectionView.ItemsSource = _viewModels;

        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;
        if (staggeredItemsLayout.Span > 0)
        {
            await RefreshAsync();
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }
}