using CommunityToolkit.Mvvm.Messaging;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story timeline feed view model: first-page loading, feed cursor paging
// and post deletion sync.
public partial class KakaoTimelinePageViewModel : BaseTimelinePageViewModel, IRecipient<ValueDeletedMessage<PostData>>
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMorePostsToLoad;
    private string _nextSince;

    public KakaoTimelinePageViewModel() => WeakReferenceMessenger.Default.Register(this);

    public void Receive(ValueDeletedMessage<PostData> message)
    {
        var viewModels = Items.OfType<KakaoPostViewModel>().Where(x => x.PostData.id == message.Value.id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);

        OnPropertyChanged(nameof(IsEmpty));
    }

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

            var timeline = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFeed(null));
            if (timeline?.feeds == null)
            {
                Items = [];
                return;
            }

            var viewModels = timeline.feeds.Select(x => KakaoStoryUtils.CreatePostViewModel(x, this)).Where(x => x != null);
            _nextSince = timeline.next_since;

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

            var timeline = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFeed(_nextSince));
            if (timeline?.feeds == null)
            {
                _areThereNoMorePostsToLoad = true;
                return;
            }

            var viewModels = timeline.feeds.Select(x => KakaoStoryUtils.CreatePostViewModel(x, this)).Where(x => x != null).ToList();
            _nextSince = timeline.next_since;
            _areThereNoMorePostsToLoad = string.IsNullOrEmpty(_nextSince) || viewModels.Count == 0;

            // One reset per fetched page instead of one incremental change per post.
            if (viewModels.Count > 0) Items = new ObservableCollection<BasePostViewModel>([.. Items, .. viewModels]);
        }
        finally { _fetchSemaphore.Release(); }
    }
}
