using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Services;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Extras;

// Bulk post management state: the signed-in account's post grid with paging, selection
// tracking, and the selection-based discovery option change / delete operations.
public sealed partial class BulkPostManagePageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMorePostsToLoad;
    private bool _isApplyingSelectAll;
    private bool _isSyncingSelectAllState;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<SelectablePostViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(SelectedCountText))]
    public partial int SelectedCount { get; private set; }

    // Nullable to match the command bar toggle's bool? state; false until the first load settles it.
    [ObservableProperty]
    public partial bool? IsSelectAll { get; set; } = false;

    public bool HasSelection => SelectedCount > 0;

    public string SelectedCountText => $"{SelectedCount}개 선택됨";

    // The command bar's select-all toggle is the only writer; programmatic state updates
    // are guarded so they do not re-apply the selection to every item.
    partial void OnIsSelectAllChanged(bool? value)
    {
        if (_isSyncingSelectAllState) return;

        ApplySelectAll(value == true);
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMorePostsToLoad = false;

            var postsResult = await ExecuteRequestAsync(new GetUserPosts(CommonShared.UserId, null, Constants.PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;

                // Download this page's thumbnails before creating the items so the grid's cells
                // see a cache hit on their first measure.
                await ExecuteWithLoadingAsync(() => MediaCacheService.PrefetchTimelineMediaAsync(posts));

                Items = new ObservableCollection<SelectablePostViewModel>(posts.Select(CreateSelectablePostViewModel));
            }
            else Items = [];

            UpdateSelectionState();
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

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var postsResult = await ExecuteRequestAsync(new GetUserPosts(CommonShared.UserId, lastViewModel.PostId, Constants.PageSize));
            if (postsResult.IsSuccess)
            {
                var posts = postsResult.Value;

                // Same guarantee as RefreshAsync but without the loading overlay: the user is
                // mid-scroll and freezing the frame behind a blocking indicator feels broken.
                await MediaCacheService.PrefetchTimelineMediaAsync(posts);

                var viewModels = posts.Select(CreateSelectablePostViewModel).ToList();

                // One reset per fetched page instead of one incremental change per post.
                if (viewModels.Count > 0) Items = new ObservableCollection<SelectablePostViewModel>([.. Items, .. viewModels]);

                if (posts.Count == 0) _areThereNoMorePostsToLoad = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Retargets the selected posts to one of the allowed discovery options.
    [RelayCommand]
    private async Task ChangeDiscoveryOptionAsync()
    {
        var selectedViewModels = Items.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글을 먼저 선택해주세요."));
            return;
        }

        var rawTo = await ShowSelectionDialogAsync("변경할 공개 범위 선택", DiscoveryOptionDisplayHelper.GetConversionTargetDisplayStrings());
        if (rawTo == null) return;

        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", $"선택한 {selectedViewModels.Count}개의 게시글 공개 범위를 '{to.ToDisplayString()}'로 변경하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkChangeDiscoveryOptionByPostIds([.. selectedViewModels.Select(x => x.PostId)], to));
        if (result.IsSuccess)
        {
            foreach (var viewModel in selectedViewModels) viewModel.ApplyDiscoveryOption(to);
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 변경이 완료되었습니다."));
        }
    }

    // Deletes the selected posts and drops them from the grid.
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selectedViewModels = Items.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글을 먼저 선택해주세요."));
            return;
        }

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("삭제", $"선택한 {selectedViewModels.Count}개의 게시글을 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkDeletePostsByPostIds([.. selectedViewModels.Select(x => x.PostId)]));
        if (result.IsSuccess)
        {
            var remainingViewModels = Items.Where(x => !x.IsSelected).ToList();
            foreach (var viewModel in selectedViewModels) viewModel.SelectionChanged -= OnItemSelectionChanged;

            Items = new ObservableCollection<SelectablePostViewModel>(remainingViewModels);
            UpdateSelectionState();
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 삭제가 완료되었습니다."));
        }
    }

    private SelectablePostViewModel CreateSelectablePostViewModel(PostResponseDto post)
    {
        var viewModel = new SelectablePostViewModel(post, this);
        viewModel.SelectionChanged += OnItemSelectionChanged;
        return viewModel;
    }

    private void OnItemSelectionChanged(object sender, EventArgs e)
    {
        if (_isApplyingSelectAll) return;

        UpdateSelectionState();
    }

    private void ApplySelectAll(bool isSelectAll)
    {
        _isApplyingSelectAll = true;
        foreach (var item in Items) item.SetSelected(isSelectAll);
        _isApplyingSelectAll = false;

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        var selectedCount = Items.Count(x => x.IsSelected);
        SelectedCount = selectedCount;

        _isSyncingSelectAllState = true;
        IsSelectAll = selectedCount > 0 && selectedCount == Items.Count;
        _isSyncingSelectAllState = false;
    }

    // Bulk operations replace or remove many posts at once, so the main window's post surfaces
    // are asked to reload instead of receiving per-post change messages.
    private static void RefreshPostPages() => RefreshRequestedMessage.Send(MainWindow.Frame.XamlRoot);
}
