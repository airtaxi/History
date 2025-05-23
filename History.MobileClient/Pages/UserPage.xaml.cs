
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using Org.Apache.Http.Impl.Client;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefreshMyProfile { get; set; }

    private readonly string _userId;
    private readonly ObservableCollection<object> _viewModels = [];
    private object _lastViewModel;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public UserPage() : this(Shared.UserId)
    {
        BackImage.IsVisible = false;
        TitleLabel.Text = "내 프로필";
        SettingsImage.IsVisible = true;
    }

    public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();

        MainCollectionView.ItemsSource = _viewModels;
#if IOS
        MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

#endif
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.OfType<PostViewModel>().Where(x => x.Post.Id == message.Value.Id);
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _fetchSemaphore.WaitAsync();

            _viewModels.Clear();

            var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
            Shared.Friends = friends;

            var user = await App.ExecuteRequestAsync(new GetUser(_userId));
            if (user.IsSuccess)
            {
                _viewModels.Add(new ProfileViewModel(user.Value));
                FriendsImage.IsVisible = true;
            }
            else return;

            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(_userId));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => new PostViewModel(x, true));
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
            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(_userId, lastViewModel?.Post.Id));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => new PostViewModel(x, true));
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else return;
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnFriendsImageTapped(object sender, TappedEventArgs e)
    {
        if (_userId == Shared.UserId)
        {
            var page = new FriendsPage();
            await App.PushModalAsync(page);
        }
        else
        {
            var page = new FriendListPage(_userId);
            await App.PushModalAsync(page);
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

    private bool _isFirstLoad = true;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstLoad || (ShouldRefreshMyProfile && _userId == Shared.UserId))
        {
            if (ShouldRefreshMyProfile) ShouldRefreshMyProfile = false;
            if (_isFirstLoad) _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        var viewModel = view.BindingContext as PostViewModel;
        if (viewModel == null) return;

        if (viewModel.Post.Id == (_lastViewModel as PostViewModel)?.Post.Id)
        {
            _lastViewModel = null;
            await LoadMoreAsync();
        }
    }

    private async void OnSettingsImageTapped(object sender, TappedEventArgs e) => await DisplayAlert("안내", "제작중입니다.", "확인");

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        MainActivityIndicator.IsRunning = isLoading;
        IsEnabled = !isLoading;
    }
}