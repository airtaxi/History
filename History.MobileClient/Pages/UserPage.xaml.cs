using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Platform;
using System.Collections.ObjectModel;
using History.Commons;
using UraniumUI.Icons.MaterialSymbols;
using History.Commons.Api.Message;
using History.MobileClient.ThirdParty.StaggeredLayout;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
    public static bool ShouldRefresh { get; set; }
    public static bool ShouldRefreshKakaoStory { get; set; }
    public string UserId { get; }
    public string KakaoUserId { get; private set; }

    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMorePostsToLoad;
    private bool _useGridLayout = true;
    private PeriodicTimer _scrollPositionTimer;
    private bool _lastScrollToTopBorderVisible;
#if IOS
    private double _lastScrollOffsetY;
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif
    private object _lastViewModel;
    private BaseProfileViewModel _viewModel;
    private readonly bool _isMyProfile;
    private readonly ObservableCollection<BasePostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);
    private string _nextSince;

    public UserPage() : this(Shared.UserId, false, true)
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

    public UserPage(string userId) : this(userId, false) { }

    public UserPage(string userId, bool isKakaoStoryMode, bool showPillGrid = false)
    {
        if (isKakaoStoryMode) KakaoUserId = userId;
        else UserId = userId;

        _isKakaoStoryMode = isKakaoStoryMode;
        InitializeComponent();

        PillGrid.IsVisible = showPillGrid;

        BanImage.IsVisible = !IsMyProfilePage;
        MemoImage.IsVisible = !_isKakaoStoryMode && !IsMyProfilePage;
        MessageImage.IsVisible = !IsMyProfilePage;

        if (_isKakaoStoryMode)
        {
            // Kakao Story profile: only back/layout/ban/friends remain; History-only header actions are hidden.
            MessageImage.IsVisible = false;
            MemoImage.IsVisible = false;
            SettingsImage.IsVisible = false;
            TitleLabel.Text = "프로필";
        }

        UpdatePillVisuals();

        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostData>>(this, OnKakaoPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
#if ANDROID
        WeakReferenceMessenger.Default.Register<TimelineVirtualizationChangedMessage>(this, OnTimelineVirtualizationChangedMessageReceived);
#endif
#if IOS
        WeakReferenceMessenger.Default.Register<TabBarHeightChangedMessage>(this, OnTabBarHeightChangedMessageReceived);

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);

        // Capture the original XAML margins before any tab bar inset is applied.
        _scrollToTopBorderBaseMargin = ScrollToTopBorder.Margin;
        _writePostBorderBaseMargin = WritePostBorder.Margin;
#endif
    }

    private bool IsMyProfilePage => _isKakaoStoryMode ? KakaoUserId == Shared.KakaoUserId : UserId == Shared.UserId;

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    private void OnKakaoPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostData> message)
    {
        var viewModels = _viewModels.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
    }

    private static string GetPostId(BasePostViewModel viewModel)
    {
        return viewModel.RepostId ?? viewModel switch
        {
            HistoryPostViewModel historyViewModel => historyViewModel.Post.Id,
            KakaoPostViewModel kakaoViewModel => kakaoViewModel.PostData.id,
            _ => null
        };
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

                try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
                catch (Exception) { }

                await Task.Delay(100);
            }

            _viewModels.Clear();
            _areThereNoMorePostsToLoad = false;
            _nextSince = null;

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode)
            {
                if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

                var profileObject = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(KakaoUserId, null));
                // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (profileObject?.profile == null)
                {
                    await DisplayAlertAsync("오류", "카카오스토리 프로필을 불러오지 못했습니다.", Constants.PromptOk);
                    return;
                }

                _viewModel = new KakaoProfileViewModel(profileObject.profile, profileObject.mutual_friend);
                ProfileDataTemplatePresenter.ViewModel = _viewModel;
                FriendsImage.IsVisible = (_viewModel as KakaoProfileViewModel)?.IsFriendsVisible ?? false;
                MessageImage.IsVisible = !IsMyProfilePage && profileObject.profile.message_sendable;

                var viewModels = (profileObject.activities ?? []).Select(KakaoStoryUtils.CreatePostViewModel).Where(x => x != null).ToList();
                // The profile feed has no next_since; the cursor is the last activity id,
                // advanced only while more than 15 items are returned (Kakao Story Manager Plus pattern).
                _nextSince = (profileObject.activities ?? []).Count > 15 ? profileObject.activities.LastOrDefault()?.id : null;
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else
            {
                var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
                Shared.Friends = friends;

                var user = await App.ExecuteRequestAsync(new GetUser(UserId));
                // The mode can change while the profile loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (user.IsSuccess)
                {
                    _viewModel = new HistoryProfileViewModel(user.Value);
                    ProfileDataTemplatePresenter.ViewModel = _viewModel;
                }
                else
                {
                    await App.PopAsync();
                    return;
                }

                var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, null, _useGridLayout ? 50 : 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)new HistoryPostViewModel(x, PostType.Timeline));
                    _lastViewModel = viewModels.LastOrDefault();
                    foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
                }
            }
        }
        catch (Exception exception)
        {
            // History errors are surfaced by the shared request pipeline; only Kakao Story shows its own alert.
            if (_isKakaoStoryMode) await DisplayAlertAsync("오류", $"카카오스토리 프로필을 불러오지 못했습니다.\n{exception.Message}\n{exception.StackTrace}", Constants.PromptOk);
            else throw;
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

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode)
            {
                var profileObject = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(KakaoUserId, _nextSince));
                // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (profileObject?.activities == null)
                {
                    _areThereNoMorePostsToLoad = true;
                    return;
                }

                var activities = profileObject.activities;
                var viewModels = activities.Select(KakaoStoryUtils.CreatePostViewModel).Where(x => x != null).ToList();
                // The profile feed has no next_since; the cursor is the last activity id,
                // advanced only while more than 15 items are returned (Kakao Story Manager Plus pattern).
                _nextSince = activities.Count > 15 ? activities.LastOrDefault()?.id : null;
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = string.IsNullOrEmpty(_nextSince) || viewModels.Count == 0;
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
            else
            {
                var lastViewModel = _viewModels.OfType<HistoryPostViewModel>().LastOrDefault();
                if (lastViewModel == null) return;

                var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
                var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, lastPostId, _useGridLayout ? 50 : 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)new HistoryPostViewModel(x, PostType.Timeline));
                    _lastViewModel = viewModels.LastOrDefault();
                    _areThereNoMorePostsToLoad = !viewModels.Any();
                    foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnFriendsImageTapped(object sender, TappedEventArgs e)
    {
        if (_isKakaoStoryMode)
        {
            var page = new FriendListPage(KakaoUserId, true);
            await App.PushAsync(page);
        }
        else
        {
            var page = new FriendListPage(UserId);
            await App.PushAsync(page);
        }
    }

    private async void OnBanUserImageTapped(object sender, TappedEventArgs e) => await _viewModel.HandleBanAsync();

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (_useGridLayout) return;

        var staggeredItemsLayout = MainCollectionView.ItemsLayout as StaggeredItemsLayout;

        var previousSpan = staggeredItemsLayout?.Span ?? 1;
        var newSpan = ((int)Width / 700) + 1;
        if (newSpan != previousSpan)
        {
            if (newSpan == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };

            WeakReferenceMessenger.Default.Send(new SpanChangedMessage());
        }
    }

    private bool _isFirstLoad = true;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = PollScrollPositionAsync(_scrollPositionTimer);

#if ANDROID
        // Apply virtualization setting once the handler is ready.
        Dispatcher.Dispatch(ApplyVirtualizationSetting);
#endif

        if (!_isKakaoStoryMode && UserId != Shared.UserId) _ = MarkFriendNotificationsAsReadAsync();

        if (_isFirstLoad || (ShouldRefresh && !_isKakaoStoryMode && UserId == Shared.UserId) || (ShouldRefreshKakaoStory && _isKakaoStoryMode && KakaoUserId == Shared.KakaoUserId))
        {
            ShouldRefresh = false;
            ShouldRefreshKakaoStory = false;
            _isFirstLoad = false;
            Dispatcher.Dispatch(async () => await RefreshAsync());
        }

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

#if IOS
        // Only apply tab bar inset when the tab bar is visible (my profile).
        // Other users' profiles hide the tab bar (Shell.TabBarIsVisible="False"),
        // so the floating borders and collection footer must not be offset.
        if (_isMyProfile)
        {
            var tabBarHeight = LayoutHelper.GetTabBarHeight();

            ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + tabBarHeight);
            WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + tabBarHeight);

            MainCollectionView.Footer = new Grid { HeightRequest = tabBarHeight };
        }
#endif
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
        _scrollPositionTimer?.Dispose();
        _scrollPositionTimer = null;
    }

#if IOS
    private void OnTabBarHeightChangedMessageReceived(object recipient, TabBarHeightChangedMessage message)
    {
        if (!_isMyProfile) return;

        MainCollectionView.Footer = new Grid { HeightRequest = message.Value };

        ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + message.Value);
        WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + message.Value);
    }
#endif

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
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

#if ANDROID
    private void OnTimelineVirtualizationChangedMessageReceived(object recipient, TimelineVirtualizationChangedMessage message) => ApplyVirtualizationSetting();

    private void ApplyVirtualizationSetting()
    {
        // Grid mode uses small thumbnails where virtualization is always desired.
        // Non-grid (timeline) mode honors the user's virtualization preference.
        var isEnabled = _useGridLayout || (Configuration.GetValue<bool?>("TimelineVirtualizationEnabled") ?? false);
        if (MainCollectionView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView recyclerView)
            recyclerView.SetItemViewCacheSize(isEnabled ? 2 : 10);
    }
#endif

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        if (view.BindingContext is not BasePostViewModel viewModel) return;

        if (_lastViewModel != null && GetPostId(viewModel) == GetPostId(_lastViewModel as BasePostViewModel))
        {
            _lastViewModel = null;
            await LoadMoreAsync();
        }
    }

#if IOS
    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();
#else
    // Not used on Android, but required for compatibility
    private void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) { }
#endif

    private async void OnTitleLabelTapped(object sender, TappedEventArgs e) => await RefreshAsync();

    private async void OnPostPinnedMessageReceived(object recipient, PostPinnedMessage message)
    {
        if (_isInForeground) await RefreshAsync();
        else ShouldRefresh = true;
    }

    private async void OnSettingsImageTapped(object sender, TappedEventArgs e)
    {
        var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetMyProfile());
        await App.PushAsync(new SettingsPage(user));
    }

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e)
    {
        if (_isKakaoStoryMode)
        {
            var proceed = await DisplayAlertAsync("안내", KakaoStoryUtils.KakaoOnlyWriteGuideMessage, "작성", Constants.PromptCancel);
            if (!proceed) return;
            await App.PushAsync(new EditPostPage(isKakaoOnlyWrite: true));
        }
        else await App.PushAsync(new EditPostPage());
    }

    private async void OnHistoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(false);

    private async void OnKakaoStoryPillTapped(object sender, TappedEventArgs e) => await SwitchModeAsync(true);

    private async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        if (isKakaoStoryMode && KakaoUserId == null)
        {
            // The pill is only visible on my profile; the Kakao Story user id is
            // resolved from the saved session so the profile feed can be fetched.
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;
            KakaoUserId = Shared.KakaoUserId;
            if (KakaoUserId == null)
            {
                await DisplayAlertAsync("오류", "카카오스토리 사용자 정보를 불러오지 못했습니다.", Constants.PromptOk);
                return;
            }
        }

        await _switchSemaphore.WaitAsync();
        try
        {
            // Another tap may have applied this mode already while we waited.
            if (_isKakaoStoryMode == isKakaoStoryMode) return;
            _isKakaoStoryMode = isKakaoStoryMode;

            UpdatePillVisuals();

            ShouldRefresh = false;
            ShouldRefreshKakaoStory = false;

            await RefreshAsync();
        }
        finally { _switchSemaphore.Release(); }
    }

    private void UpdatePillVisuals()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color ?? Colors.Orange;
        var isDarkTheme = Utils.GetGlobalAppTheme() == AppTheme.Dark;
        var inactiveBackgroundColor = isDarkTheme ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xEA, 0xEA, 0xEA);
        var inactiveTextColor = isDarkTheme ? Color.FromRgb(0xAA, 0xAA, 0xAA) : Color.FromRgb(0x66, 0x66, 0x66);

        HistoryPillBorder.BackgroundColor = _isKakaoStoryMode ? inactiveBackgroundColor : primaryColor;
        HistoryPillLabel.TextColor = _isKakaoStoryMode ? inactiveTextColor : Colors.White;
        KakaoStoryPillBorder.BackgroundColor = _isKakaoStoryMode ? primaryColor : inactiveBackgroundColor;
        KakaoStoryPillLabel.TextColor = _isKakaoStoryMode ? Colors.White : inactiveTextColor;
    }

    private async Task PollScrollPositionAsync(PeriodicTimer timer)
    {
        while (await timer.WaitForNextTickAsync())
        {
            var scrollOffsetY = MainCollectionView.GetScrollOffsetY();
            var shouldShow = scrollOffsetY > 0;
            if (shouldShow != _lastScrollToTopBorderVisible)
            {
                ScrollToTopBorder.IsVisible = shouldShow;
                _lastScrollToTopBorderVisible = shouldShow;
            }
        }
    }

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e)
    {
        var firstViewModel = _viewModels.FirstOrDefault();
        if (firstViewModel == null) return;

        try { MainCollectionView.ScrollTo(firstViewModel, null, ScrollToPosition.Start, false); }
        catch (Exception) { }

        // Hide immediately so the border does not linger until the next 1-second polling tick.
        ScrollToTopBorder.IsVisible = false;
        _lastScrollToTopBorderVisible = false;
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
            var span = ((int)Width / 700) + 1;
            if (span == 1) MainCollectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            else MainCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = span };

#if ANDROID
            // Re-apply the virtualization preference after switching to the non-grid layout.
            Dispatcher.Dispatch(ApplyVirtualizationSetting);
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

#if ANDROID
            // Restore default virtualization for the grid layout.
            Dispatcher.Dispatch(ApplyVirtualizationSetting);
#endif
        }
    }

    private async void OnMessageImageTapped(object sender, TappedEventArgs e)
    {
        if (_isKakaoStoryMode)
        {
            var nickname = (_viewModel as KakaoProfileViewModel)?.Nickname;
            if (string.IsNullOrEmpty(nickname)) return;

            await App.PushModalAsync(new WriteMessagePage(KakaoUserId, nickname, true));
        }
        else
        {
            var canSendMessage = await App.ExecuteRequestAsync(new CheckMessagePermission(UserId));
            if (canSendMessage.IsSuccess) await App.PushModalAsync(new WriteMessagePage(UserId, (_viewModel as HistoryProfileViewModel)?.User.Nickname));
        }
    }
}
