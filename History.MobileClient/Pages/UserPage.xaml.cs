
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefreshMyProfile { get; set; }

    private readonly string _userId;
    private ObservableCollection<object> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public UserPage() : this(Shared.UserId)
    {
    }

    public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModel = _viewModels.OfType<PostViewModel>().FirstOrDefault(x => x.Post.Id == message.Value.Id);
        if (viewModel == null) return;

        _viewModels.Remove(viewModel);
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            _viewModels.Clear();

            var user = await App.ExecuteRequestAsync(new GetUser(_userId));
            if (user.IsSuccess) _viewModels.Add(new ProfileViewModel(user));
            else return;

            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(_userId));
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

            var lastViewModel = _viewModels.OfType<PostViewModel>().LastOrDefault();
            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(_userId, lastViewModel?.Post.Id));
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

        if (ShouldRefreshMyProfile && _userId == Shared.UserId)
        {
            ShouldRefreshMyProfile = false;
            await RefreshAsync();
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private Border _lastElement;
    private void OnChildAdded(object sender, ElementEventArgs e)
    {
        var border = e.Element as Border;
        var viewModel = border.BindingContext as PostViewModel;
        if (viewModel == _viewModels.LastOrDefault())
        {
            _lastElement?.Loaded -= OnLastItemBorderLoaded;
            border.Loaded += OnLastItemBorderLoaded;
            _lastElement = border;
        }
    }

    private async void OnLastItemBorderLoaded(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount > 0)
        {
            _lastElement?.Loaded -= OnLastItemBorderLoaded;
            await LoadMoreAsync();
        }
    }
}