using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using History.Commons;
using UraniumUI.Icons.MaterialSymbols;
using History.Commons.Api.Message;




#if ANDROID
using History.MobileClient.ThirdParty.StaggeredLayout;
#endif

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefresh { get; set; }
    public string UserId { get; }

    private bool _isInForeground;
    private bool _areThereNoMorePostsToLoad;
    private bool _useGridLayout = true;
#if IOS
    private double _lastScrollOffsetY;
#endif
    private object _lastViewModel;
    private ProfileViewModel _viewModel;
    private readonly bool _isMyProfile;
    private readonly ObservableCollection<PostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

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
		UserId = userId;
        InitializeComponent();

        if (UserId == Shared.UserId)
        {
            BanImage.IsVisible = false;
            MemoImage.IsVisible = false;
        }
        else MessageImage.IsVisible = true;

        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

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

            var user = await App.ExecuteRequestAsync(new GetUser(UserId));
            if (user.IsSuccess)
            {
                _viewModel = new ProfileViewModel(user.Value);
                ProfileDataTemplatePresenter.ViewModel = _viewModel;
            }
            else
            {
                await App.PopAsync();
                return;
            }

            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, null, _useGridLayout ? 50 : 30));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => new PostViewModel(x, PostType.Timeline));
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
            var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, lastPostId, _useGridLayout ? 50 : 30));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;
                var viewModels = posts.Select(x => new PostViewModel(x, PostType.Timeline));
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = !viewModels.Any();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnFriendsImageTapped(object sender, TappedEventArgs e)
    {
        var page = new FriendListPage(UserId);
        await App.PushAsync(page);
    }

    private async void OnBanUserImageTapped(object sender, TappedEventArgs e) => await _viewModel.HandleBanAsync();

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (_useGridLayout) return;

#if ANDROID
        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;

        var previousSpan = staggeredItemsLayout?.Span ?? 1;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            if (newSpan == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
        }
#endif
    }

    private bool _isFirstLoad = true;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        if (UserId != Shared.UserId) _ = MarkFriendNotificationsAsReadAsync();

        if (_isFirstLoad || (ShouldRefresh && UserId == Shared.UserId))
        {
            ShouldRefresh = false;
            _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    private async Task MarkFriendNotificationsAsReadAsync()
    {
        var success = await Shared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByFriendUserId(UserId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationFriendUserReadMessage(UserId));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

#if IOS
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        MainCollectionView.SetScrollOffsetY(_lastScrollOffsetY, false);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);

        _lastScrollOffsetY = MainCollectionView.GetScrollOffsetY();
    }

#endif
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
    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e) => await App.PushAsync(new EditPostPage());

    private void OnMainCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var collectionView = sender as CollectionView;
        if (collectionView.GetScrollOffsetY() > 0) ScrollToTopBorder.IsVisible = true;
        else ScrollToTopBorder.IsVisible = false;
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_isMyProfile) return false;

        _ = App.PopAsync();
        return true;
    }
    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        if (!_isMyProfile)
        {
            AppleSwipeGestureHelper.ApplyToPage(this);
        }
#endif
    }

    private async void OnMemoImageTapped(object sender, TappedEventArgs e)
    {
        var memo = await DisplayPromptAsync("메모 작성", "사용자 메모를 작성해주세요. 공란으로 설정 시 메모가 삭제됩니다.", Constants.PromptOk, Constants.PromptCancel, "최대 10자까지 입력 가능. 공란 시 삭제", CommonsConstants.MaxMemoLength, keyboard: Keyboard.Text);
        if (memo == null) return;

        var response = await App.ExecuteRequestAsync(new UpdateMemo(UserId, memo.Trim()));
        if (response.IsSuccess) await _viewModel.RefreshAsync();
    }

    private void OnLayoutImageTapped(object sender, TappedEventArgs e)
    {
        _useGridLayout = !_useGridLayout;

        if (!_useGridLayout)
        {
            LayoutFontImageSource.Glyph = MaterialSharp.Dataset;

            MainCollectionView.ItemTemplate = App.Current.Resources["TimelineTemplateSelector"] as DataTemplateSelector;
#if IOS
            MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
#else
            var span = ((int)Width / 700) + 1;
            if (span == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = span };
#endif
        }
        else
        {
            LayoutFontImageSource.Glyph = MaterialSharp.Lists;

            MainCollectionView.ItemTemplate = App.Current.Resources["PostPreviewTemplate"] as DataTemplate;
            MainCollectionView.ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                Span = 3,
                HorizontalItemSpacing = 1,
                VerticalItemSpacing = 1
            };
        }
    }

    private async void OnMessageImageTapped(object sender, TappedEventArgs e)
    {
        var canSendMessage = await App.ExecuteRequestAsync(new CheckMessagePermission(UserId));
        if (canSendMessage.IsSuccess) await App.PushModalAsync(new WriteMessagePage(UserId, _viewModel.User.Nickname));
    }
}