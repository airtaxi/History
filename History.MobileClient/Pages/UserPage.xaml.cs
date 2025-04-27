
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.MobileClient.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
	private readonly string _userId;
    private ObservableCollection<object> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public UserPage()
	{
		_userId = Shared.UserId;
		InitializeComponent();
    }

    public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();
    }

    private async Task InitializeAsync()
    {
        MainCollectionView.ItemsSource = _viewModels;

        try
        {
            await _fetchSemaphore.WaitAsync();
            IsEnabled = false;
            IsBusy = true;

            _viewModels.Clear();

            var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetUser(_userId));
            _viewModels.Add(new ProfileViewModel(user));

            //var posts = await Shared.ApiHandler.ExecuteRequestAsync(new GetUserPosts(_userId));
            //var postViewModels = posts.Select(x => new PostViewModel(x));
            //foreach (var postViewModel in postViewModels) _viewModels.Add(postViewModel);
        }
        finally
        {
            IsEnabled = true;
            IsBusy = false;
            _fetchSemaphore.Release();
        }
    }

    private async Task LoadMoreAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();
            IsEnabled = false;
            IsBusy = true;

            var lastPost = _viewModels.OfType<PostViewModel>().LastOrDefault();
            var posts = await Shared.ApiHandler.ExecuteRequestAsync(new GetUserPosts(_userId, lastPost?.Post.Id));
            var postViewModels = posts.Select(x => new PostViewModel(x));
            foreach (var postViewModel in postViewModels) _viewModels.Add(postViewModel);
        }
        finally
        {
            IsEnabled = true;
            IsBusy = false;
            _fetchSemaphore.Release();
        }
    }

    private async void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;
        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
            await InitializeAsync();
        }
    }

    private bool _isInitialized = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isInitialized)
        {
            await InitializeAsync();
            _isInitialized = true;
        }
    }
}