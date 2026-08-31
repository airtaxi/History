using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Timeline feed view model mirroring the MAUI TimelineViewModel's History branch:
// first-page loading, infinite scroll pagination and post deletion sync.
public partial class TimelinePageViewModel : BaseViewModel, IRecipient<ValueDeletedMessage<PostResponseDto>>
{
    private const int PageSize = 30;

    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMorePostsToLoad;

    [ObservableProperty]
    public partial ObservableCollection<BasePostViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public TimelinePageViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = Items.OfType<HistoryPostViewModel>().Where(x => x.Post.Id == message.Value.Id).ToList(); // ToList is needed (Collection will be modified)
        foreach (var viewModel in viewModels) Items.Remove(viewModel);

        OnPropertyChanged(nameof(IsEmpty));
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            Items.Clear();
            _areThereNoMorePostsToLoad = false;

            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(null, PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
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

            var postsResult = await App.ExecuteRequestAsync(new GetTimelinePosts(lastViewModel.RepostId ?? lastViewModel.Post.Id, PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value.Where(x => !x.IsRepost || (x.IsRepost && x.ParentPost != null));
                var viewModels = posts.Select(x => (BasePostViewModel)(x.IsRepost ? new HistoryRepostViewModel(x.Id, x.ParentPost, x.User, this) : new HistoryPostViewModel(x, PostType.Timeline, this)));
                foreach (var viewModel in viewModels) Items.Add(viewModel);

                if (postsResult.Value.Count == 0) _areThereNoMorePostsToLoad = true;
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally { _fetchSemaphore.Release(); }
    }
}