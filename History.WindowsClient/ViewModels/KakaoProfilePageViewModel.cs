using CommunityToolkit.Mvvm.Messaging;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story profile page view model: profile feed loading, activity cursor paging
// and post deletion sync.
public partial class KakaoProfilePageViewModel : BaseProfilePageViewModel, IRecipient<ValueDeletedMessage<PostData>>
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMorePostsToLoad;
    private string _kakaoUserId;
    private string _nextSince;

    public override string UserId => _kakaoUserId;

    public KakaoProfilePageViewModel() => WeakReferenceMessenger.Default.Register(this);

    public void Receive(ValueDeletedMessage<PostData> message)
    {
        var viewModels = Items.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);

        OnPropertyChanged(nameof(IsEmpty));
    }

    // Stores the navigation parameter only (XamlRoot-independent, called from
    // OnNavigatedTo); the actual loading runs from OnLoaded.
    public override void Initialize(string kakaoUserId) => _kakaoUserId = kakaoUserId;

    public override async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMorePostsToLoad = false;
            _nextSince = null;

            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            var profileObject = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(_kakaoUserId, null));
            if (profileObject?.profile == null)
            {
                await ShowMessageDialogAsync(new("오류", "카카오스토리 프로필을 불러오지 못했습니다."));
                return;
            }

            Profile = new KakaoProfileViewModel(profileObject.profile, profileObject.mutual_friend, this);

            var activities = profileObject.activities ?? [];
            var viewModels = activities.Select(x => KakaoStoryUtils.CreatePostViewModel(x, this)).Where(x => x != null).ToList();

            // The profile feed has no next_since cursor: the last activity id advances the
            // cursor only while a full page (> 15) was returned.
            _nextSince = activities.Count > 15 ? activities.LastOrDefault()?.id : null;

            // Swap the whole collection once so the repeater sees a single reset
            // instead of one incremental change per post.
            Items = new ObservableCollection<BasePostViewModel>(viewModels);
        }
        finally { _fetchSemaphore.Release(); }
    }

    public override async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            // Only a full page advances the cursor, so a short feed has nothing more to load.
            if (string.IsNullOrEmpty(_nextSince))
            {
                _areThereNoMorePostsToLoad = true;
                return;
            }

            var profileObject = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(_kakaoUserId, _nextSince));
            if (profileObject?.activities == null)
            {
                _areThereNoMorePostsToLoad = true;
                return;
            }

            var activities = profileObject.activities;
            var viewModels = activities.Select(x => KakaoStoryUtils.CreatePostViewModel(x, this)).Where(x => x != null).ToList();
            _nextSince = activities.Count > 15 ? activities.LastOrDefault()?.id : null;
            _areThereNoMorePostsToLoad = string.IsNullOrEmpty(_nextSince) || viewModels.Count == 0;

            // One reset per fetched page instead of one incremental change per post.
            if (viewModels.Count > 0) Items = new ObservableCollection<BasePostViewModel>([.. Items, .. viewModels]);
        }
        finally { _fetchSemaphore.Release(); }
    }
}
