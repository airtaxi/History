using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Profile page view model: profile loading, the user's post feed with
// infinite-scroll pagination, and post deletion/pin sync.
public partial class ProfilePageViewModel : BaseViewModel,
    IRecipient<ValueDeletedMessage<PostResponseDto>>,
    IRecipient<PostPinnedMessage>
{
    private const int PageSize = 30;

    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMorePostsToLoad;
    private string _userId;

    [ObservableProperty]
    public partial BaseProfileViewModel Profile { get; private set; }

    [ObservableProperty]
    public partial ObservableCollection<BasePostViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    // Stores the navigation parameter only (XamlRoot-independent, called from
    // OnNavigatedTo); the actual loading runs from OnLoaded.
    public void Initialize(string userId) => _userId = userId;

    public ProfilePageViewModel()
    {
        WeakReferenceMessenger.Default.Register((IRecipient<ValueDeletedMessage<PostResponseDto>>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<PostPinnedMessage>)this);
    }

    public void Receive(ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void Receive(PostPinnedMessage message) => _ = RefreshAsync();

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            Items.Clear();
            _areThereNoMorePostsToLoad = false;

            // Refresh the shared friend cache first: the friendship/favorite surfaces
            // elsewhere read from it.
            var friendsResult = await ExecuteRequestAsync(new GetFriends(CommonShared.UserId));
            if (friendsResult.IsSuccess) CommonShared.Friends = friendsResult.Value;

            var userResult = await ExecuteRequestAsync(new GetUser(_userId));
            if (userResult.IsFailure)
            {
                OnPropertyChanged(nameof(IsEmpty));

                // The profile cannot be shown for an unknown/blocked user, so leave the page.
                await TryNavigateBackAsync();
                return;
            }
            Profile = new HistoryProfileViewModel(userResult.Value, this);

            var postsResult = await ExecuteRequestAsync(new GetUserPosts(_userId, null, PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null)).ToList();

                // Download this page's carousel images before creating the post view models so
                // every carousel sees a cache hit and its height is final on the first measure.
                await ExecuteWithLoadingAsync(() => MediaCacheService.PrefetchTimelineMediaAsync(posts));

                var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User, this) : new HistoryPostViewModel(x, PostType.Timeline, this)));
                foreach (var viewModel in viewModels) Items.Add(viewModel);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.OfType<HistoryPostViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var postsResult = await ExecuteRequestAsync(new GetUserPosts(_userId, lastViewModel.RepostId ?? lastViewModel.Post.Id, PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null)).ToList();

                // Same guarantee as RefreshAsync but without the loading overlay: the user is
                // mid-scroll and freezing the frame behind a blocking indicator feels broken.
                await MediaCacheService.PrefetchTimelineMediaAsync(posts);

                var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User, this) : new HistoryPostViewModel(x, PostType.Timeline, this)));
                foreach (var viewModel in viewModels) Items.Add(viewModel);

                if (postsResult.Value.Count == 0) _areThereNoMorePostsToLoad = true;
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Viewing a friend's profile clears that friend's notifications and broadcasts the
    // read state so notification surfaces can drop their badges.
    public async Task MarkFriendNotificationsAsReadAsync()
    {
        if (_userId == CommonShared.UserId) return;

        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByFriendUserId(_userId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationFriendUserReadMessage(_userId));
    }
}
