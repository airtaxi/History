using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Message;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using System.Collections.ObjectModel;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
using Application = Microsoft.Maui.Controls.Application;

namespace History.MobileClient.ViewModels;

// Blazor user profile view model, ported from UserPage.xaml.cs. Owns the profile feed
// collection, the loaded BaseProfileViewModel, and the load/refresh/switch/layout-toggle
// logic; the Blazor profile renders Items + ProfileVm and the native chrome binds to the
// header/loading/scroll-to-top state. The legacy UserPage (dead code) keeps the static
// ShouldRefresh flags that other pages still set.
public partial class UserProfileViewModel : ObservableObject
{
    private bool _isInForeground;
    private bool _isKakaoStoryMode;
    private bool _areThereNoMorePostsToLoad;
    private bool _isFirstLoad = true;
    private string _nextSince;
    private BasePostViewModel _lastViewModel;
    private readonly bool _isMyProfile;
    private readonly bool _showPillGrid;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly SemaphoreSlim _switchSemaphore = new(1, 1);

    public string UserId { get; }
    public string KakaoUserId { get; private set; }

    public ObservableCollection<BasePostViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsScrollToTopVisible { get; set; }

    [ObservableProperty]
    public partial BaseProfileViewModel ProfileVm { get; private set; }

    [ObservableProperty]
    public partial bool UseGridLayout { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsKakaoStoryMode { get; private set; }

    // Header surface bound by the native chrome.
    [ObservableProperty]
    public partial bool IsBackVisible { get; private set; }

    [ObservableProperty]
    public partial string TitleText { get; private set; }

    [ObservableProperty]
    public partial bool IsMessageVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsMemoVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsFriendsVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsBanVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsWritePostVisible { get; private set; }

    public bool ShowPillGrid => _showPillGrid;

    // True only for the parameterless tab constructor (History my profile with the
    // tab bar visible), mirroring UserPage's _isMyProfile flag.
    public bool IsMyProfileTab => _isMyProfile;

    private bool IsMyProfilePage => _isKakaoStoryMode ? KakaoUserId == Shared.KakaoUserId : UserId == Shared.UserId;

    public UserProfileViewModel() : this(Shared.UserId, false, true)
    {
        _isMyProfile = true;

        UpdateHeaderSurface();

        WeakReferenceMessenger.Default.Register<PostPinnedMessage>(this, OnPostPinnedMessageReceived);
    }

    public UserProfileViewModel(string userId) : this(userId, false) { }

    public UserProfileViewModel(string userId, bool isKakaoStoryMode, bool showPillGrid = false)
    {
        if (isKakaoStoryMode) KakaoUserId = userId;
        else UserId = userId;

        _isKakaoStoryMode = isKakaoStoryMode;
        _showPillGrid = showPillGrid;

        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostData>>(this, OnKakaoPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<TabReselectedMessage>(this, OnTabReselectedMessageReceived);

        UpdateHeaderSurface();
    }

    private void UpdateHeaderSurface()
    {
        IsBackVisible = !_isMyProfile;
        TitleText = _isMyProfile ? "내 프로필" : "프로필";
        IsBanVisible = !IsMyProfilePage;
        IsMemoVisible = !_isKakaoStoryMode && !IsMyProfilePage;
        IsMessageVisible = !IsMyProfilePage;
        IsSettingsVisible = _isMyProfile;
        IsWritePostVisible = _isMyProfile;

        if (_isKakaoStoryMode)
        {
            // Kakao Story profile: only back/layout/ban/friends remain; History-only header actions are hidden.
            IsMessageVisible = false;
            IsMemoVisible = false;
            IsSettingsVisible = false;
        }
    }

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
        _lastViewModel = Items.LastOrDefault();
    }

    private void OnKakaoPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostData> message)
    {
        var viewModels = Items.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);
        _lastViewModel = Items.LastOrDefault();
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

            var isKakaoStoryMode = _isKakaoStoryMode;
            if (isKakaoStoryMode)
            {
                if (!await KakaoStoryUtils.EnsureLoggedInAsync(App.TopPage)) return;

                var profileObject = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(KakaoUserId, null));
                // The mode can change while the feed loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (profileObject?.profile == null)
                {
                    await App.TopPage.DisplayAlertAsync("오류", "카카오스토리 프로필을 불러오지 못했습니다.", Constants.PromptOk);
                    return;
                }

                ProfileVm = new KakaoProfileViewModel(profileObject.profile, profileObject.mutual_friend);
                IsFriendsVisible = (ProfileVm as KakaoProfileViewModel)?.IsFriendsVisible ?? false;
                IsMessageVisible = !IsMyProfilePage && profileObject.profile.message_sendable;

                var viewModels = (profileObject.activities ?? []).Select(KakaoStoryUtils.CreatePostViewModel).Where(x => x != null).ToList();
                // The profile feed has no next_since; the cursor is the last activity id,
                // advanced only while more than 15 items are returned (Kakao Story Manager Plus pattern).
                _nextSince = (profileObject.activities ?? []).Count > 15 ? profileObject.activities.LastOrDefault()?.id : null;
                _lastViewModel = viewModels.LastOrDefault();
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
            else
            {
                var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
                Shared.Friends = friends;

                var user = await App.ExecuteRequestAsync(new GetUser(UserId));
                // The mode can change while the profile loads (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (user.IsSuccess) ProfileVm = new HistoryProfileViewModel(user.Value);
                else
                {
                    await App.PopAsync();
                    return;
                }

                var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, null, UseGridLayout ? 50 : 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)new HistoryPostViewModel(x, PostType.Timeline));
                    _lastViewModel = viewModels.LastOrDefault();
                    foreach (var viewModel in viewModels) Items.Add(viewModel);
                }
            }
        }
        catch (Exception exception)
        {
            // History errors are surfaced by the shared request pipeline; only Kakao Story shows its own alert.
            if (_isKakaoStoryMode) await App.TopPage.DisplayAlertAsync("오류", $"카카오스토리 프로필을 불러오지 못했습니다.\n{exception.Message}\n{exception.StackTrace}", Constants.PromptOk);
            else throw;
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task LoadMoreAsync()
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
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }
            else
            {
                var lastViewModel = Items.OfType<HistoryPostViewModel>().LastOrDefault();
                if (lastViewModel == null) return;

                var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
                var postsResult = await App.ExecuteRequestAsync(new GetUserPosts(UserId, lastPostId, UseGridLayout ? 50 : 30));
                // The mode can change while the posts load (fast pill switching); discard the stale result, the pending switch reloads.
                if (isKakaoStoryMode != _isKakaoStoryMode) return;

                if (postsResult.IsSuccess)
                {
                    var posts = postsResult.Value;
                    var viewModels = posts.Select(x => (BasePostViewModel)new HistoryPostViewModel(x, PostType.Timeline));
                    _lastViewModel = viewModels.LastOrDefault();
                    _areThereNoMorePostsToLoad = !viewModels.Any();
                    foreach (var viewModel in viewModels) Items.Add(viewModel);
                }
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    public async Task SwitchModeAsync(bool isKakaoStoryMode)
    {
        if (_isKakaoStoryMode == isKakaoStoryMode) return;

        if (isKakaoStoryMode && KakaoUserId == null)
        {
            // The pill is only visible on my profile; the Kakao Story user id is
            // resolved from the saved session so the profile feed can be fetched.
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(App.TopPage)) return;
            KakaoUserId = Shared.KakaoUserId;
            if (KakaoUserId == null)
            {
                await App.TopPage.DisplayAlertAsync("오류", "카카오스토리 사용자 정보를 불러오지 못했습니다.", Constants.PromptOk);
                return;
            }
        }

        await _switchSemaphore.WaitAsync();
        try
        {
            // Another tap may have applied this mode already while we waited.
            if (_isKakaoStoryMode == isKakaoStoryMode) return;
            _isKakaoStoryMode = isKakaoStoryMode;
            IsKakaoStoryMode = isKakaoStoryMode;

            UserPage.ShouldRefresh = false;
            UserPage.ShouldRefreshKakaoStory = false;

            await RefreshAsync();
        }
        finally { _switchSemaphore.Release(); }
    }

    public void ToggleLayout() => UseGridLayout = !UseGridLayout;

    public async Task BackAsync() => await App.PopAsync();

    public async Task MessageAsync()
    {
        if (_isKakaoStoryMode)
        {
            var nickname = (ProfileVm as KakaoProfileViewModel)?.Nickname;
            if (string.IsNullOrEmpty(nickname)) return;

            await App.PushModalAsync(new WriteMessagePage(KakaoUserId, nickname, true));
        }
        else
        {
            var canSendMessage = await App.ExecuteRequestAsync(new CheckMessagePermission(UserId));
            if (canSendMessage.IsSuccess) await App.PushModalAsync(new WriteMessagePage(UserId, (ProfileVm as HistoryProfileViewModel)?.User.Nickname));
        }
    }

    public async Task MemoAsync()
    {
        var memo = await App.TopPage.DisplayPromptAsync("메모 작성", "사용자 메모를 작성해주세요. 공란으로 설정 시 메모가 삭제됩니다.", Constants.PromptOk, Constants.PromptCancel, "최대 10자까지 입력 가능. 공란 시 삭제", CommonsConstants.MaxMemoLength, keyboard: Keyboard.Text);
        if (memo == null) return;

        var response = await App.ExecuteRequestAsync(new UpdateMemo(UserId, memo.Trim()));
        if (response.IsSuccess) await ProfileVm.RefreshAsync();
    }

    public async Task FriendsAsync()
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

    public async Task BanAsync() => await ProfileVm.HandleBanAsync();

    public async Task SettingsAsync()
    {
        var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetMyProfile());
        await App.PushAsync(new SettingsPage(user));
    }

    public async Task WritePostAsync()
    {
        if (_isKakaoStoryMode)
        {
            var proceed = await App.TopPage.DisplayAlertAsync("안내", KakaoStoryUtils.KakaoOnlyWriteGuideMessage, "작성", Constants.PromptCancel);
            if (!proceed) return;
            await App.PushAsync(new EditPostPage(isKakaoOnlyWrite: true));
        }
        else await App.PushAsync(new EditPostPage());
    }

    public async Task OnAppearingAsync()
    {
        _isInForeground = true;

        if (!_isKakaoStoryMode && UserId != Shared.UserId) _ = MarkFriendNotificationsAsReadAsync();

        if (_isFirstLoad || (UserPage.ShouldRefresh && !_isKakaoStoryMode && UserId == Shared.UserId) || (UserPage.ShouldRefreshKakaoStory && _isKakaoStoryMode && KakaoUserId == Shared.KakaoUserId))
        {
            _isFirstLoad = false;
            UserPage.ShouldRefresh = false;
            UserPage.ShouldRefreshKakaoStory = false;
            await RefreshAsync();
        }
    }

    public void OnDisappearing() => _isInForeground = false;

    private async Task MarkFriendNotificationsAsReadAsync()
    {
        var success = await Shared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByFriendUserId(UserId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationFriendUserReadMessage(UserId));
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

    private async void OnPostPinnedMessageReceived(object recipient, PostPinnedMessage message)
    {
        if (_isInForeground) await RefreshAsync();
        else UserPage.ShouldRefresh = true;
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

    // Plain event (deliberately not INPC) consumed by the Blazor profile to scroll
    // without triggering a full re-render.
    public event Action ScrollToTopRequested;
}
