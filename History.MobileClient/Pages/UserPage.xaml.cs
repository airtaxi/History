
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefreshMyProfile { get; set; }

    private readonly string _userId;
    private readonly bool _isMyProfile;
    private ObservableCollection<object> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public UserPage() : this(Shared.UserId)
    {
        _isMyProfile = true;
        TitleGrid.IsVisible = false;
    }

    public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();

#if IOS
        MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

#endif
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

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;
        if (staggeredItemsLayout == null) return;

        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
        }
    }

    private bool _isFirstLoad = true;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MainCollectionView.ItemsSource = _viewModels;

        if (_isFirstLoad || (ShouldRefreshMyProfile && _userId == Shared.UserId))
        {
            if (ShouldRefreshMyProfile) ShouldRefreshMyProfile = false;
            if (_isFirstLoad) _isFirstLoad = false;
            await RefreshAsync();
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (_isMyProfile)
        {
            var theme = Utils.GetGlobalAppTheme();
            if (theme == AppTheme.Dark)
                CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Application.Current.Resources["OffBlack"] as Color);
            else
                CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Application.Current.Resources["White"] as Color);
        }
        else CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Application.Current.Resources["Primary"] as Color);
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
            if (_lastElement != null) _lastElement.Loaded -= OnLastItemBorderLoaded;
            border.Loaded += OnLastItemBorderLoaded;
            _lastElement = border;
        }
    }

    private async void OnLastItemBorderLoaded(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount > 0)
        {
            if (_lastElement != null) _lastElement.Loaded -= OnLastItemBorderLoaded;
            await LoadMoreAsync();
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}