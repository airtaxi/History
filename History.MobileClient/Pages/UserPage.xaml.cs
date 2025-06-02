using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefresh { get; set; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private object _lastViewModel;
    private ProfileViewModel _viewModel;
    private readonly bool _isMyProfile;
    private readonly string _userId;
    private readonly ObservableCollection<object> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
#if IOS
    private PostViewModel _tappedViewModel;
#endif

    public UserPage() : this(Shared.UserId)
    {
        _isMyProfile = true;
        BackImage.IsVisible = false;
        FriendsImage.IsVisible = false;
        TitleLabel.Text = "내 프로필";
        SettingsImage.IsVisible = true;
        WritePostBorder.IsVisible = true;
        Shell.SetTabBarIsVisible(this, true);

        WeakReferenceMessenger.Default.Register<PostPinnedMessage>(this, OnPostPinnedMessageReceived);
    }

    public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();

        if (_userId == Shared.UserId) BanImage.IsVisible = false;

        MainCollectionView.ItemsSource = _viewModels;
#if IOS
        MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
        WeakReferenceMessenger.Default.Register<ApplePostViewModelTapMessage>(this, OnApplePostViewModelTapMessageReceived);

#endif
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

#if IOS
    private void OnApplePostViewModelTapMessageReceived(object recipient, ApplePostViewModelTapMessage message)
    {
        if (_viewModels.Contains(message.Value))
        {
            _tappedViewModel = message.Value;
        }
    }

#endif
    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.OfType<PostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (_viewModels.Count > 0)
            {
                var firstViewModel = _viewModels.FirstOrDefault();
                if (firstViewModel == null) return;

                MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);

                await Task.Delay(100);
            }

            _viewModels.Clear();

            var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
            Shared.Friends = friends;

            var user = await App.ExecuteRequestAsync(new GetUser(_userId));
            if (user.IsSuccess)
            {
                _viewModel = new ProfileViewModel(user.Value);
                _viewModels.Add(_viewModel);
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
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.OfType<PostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel is RepostViewModel repostViewModel ? repostViewModel.RepostId : lastViewModel.Post.Id;
            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(_userId, lastPostId));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => new PostViewModel(x, true));
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnFriendsImageTapped(object sender, TappedEventArgs e)
    {
        var page = new FriendListPage(_userId);
        await App.PushAsync(page);
    }

    private async void OnBanUserImageTapped(object sender, TappedEventArgs e) => await _viewModel.HandleBanAsync();

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
        _isInForeground = true;

#if IOS
        Debug.WriteLine($"[TL] OnAppearing {_tappedViewModel}");
        if (_tappedViewModel != null)
        {
            Dispatcher.Dispatch(() =>
            {
                MainCollectionView.ScrollTo(_tappedViewModel, null, ScrollToPosition.Start, false);
                _tappedViewModel = null;
            });
        }

#endif
        if (_isFirstLoad || (ShouldRefresh && _userId == Shared.UserId))
        {
            ShouldRefresh = false;
            _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewChildAdded(object sender, ElementEventArgs e)
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

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        await LoadMoreAsync();
    }

    private async void OnTitleLabelTapped(object sender, TappedEventArgs e) => await RefreshAsync();

    private async void OnPostPinnedMessageReceived(object recipient, PostPinnedMessage message)
    {
        if (_isInForeground) await RefreshAsync();
        else ShouldRefresh = true;
    }

    private async void OnSettingsImageTapped(object sender, TappedEventArgs e) => await App.PushAsync(new SettingsPage(_viewModel.User));
    private async void OnWritePostImageTapped(object sender, TappedEventArgs e) => await App.PushAsync(new EditPostPage());

    private double _lastVerticalOffset = 0;
    private double _topVerticalOffset = 0;
    private void OnMainCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        if (e.VerticalOffset > _topVerticalOffset) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;
        _lastVerticalOffset = e.VerticalOffset;
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel != null) MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);
        _topVerticalOffset = _lastVerticalOffset;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_isMyProfile) return false;

        _ = App.PopAsync();
        return true;
    }
    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}