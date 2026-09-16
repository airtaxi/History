using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.Friendship;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels.Extras;

// Batch friend deletion state: the signed-in account's Kakao Story friend list with
// selection tracking, the selection helpers, and the sequential delete loop (the API has
// no bulk endpoint) with progress and cancellation.
public sealed partial class BatchDeleteFriendsPageViewModel : BaseViewModel
{
    private const int DeleteDelayMilliseconds = 100;

    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isApplyingBulkSelection;
    private bool _isLeavingPage;
    private bool _isSyncingSelectAllState;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<SelectableKakaoFriendViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(SelectedCountText))]
    public partial int SelectedCount { get; private set; }

    // Nullable to match the command bar toggle's bool? state; false until the first load settles it.
    [ObservableProperty]
    public partial bool? IsSelectAll { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDeleting))]
    public partial bool IsDeleting { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteProgressText))]
    public partial double DeleteProgress { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteProgressText))]
    public partial double DeleteTotal { get; private set; }

    public bool HasSelection => SelectedCount > 0;

    public bool IsNotDeleting => !IsDeleting;

    public string SelectedCountText => $"{SelectedCount}명 선택됨";

    public string DeleteProgressText => $"{DeleteProgress:0}/{DeleteTotal:0}";

    // The command bar's select-all toggle is the only writer; programmatic state updates
    // are guarded so they do not re-apply the selection to every item.
    partial void OnIsSelectAllChanged(bool? value)
    {
        if (_isSyncingSelectAllState) return;

        SetSelection(Items, value == true);
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        if (IsDeleting) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

            FriendData.Friends friends;
            try { friends = await ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFriends()); }
            catch (Exception exception)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 친구 목록을 불러오지 못했습니다.\n{exception.Message}"));
                return;
            }

            if (friends?.profiles == null)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "카카오스토리 친구 목록을 불러오지 못했습니다."));
                return;
            }

            _isLeavingPage = false;

            // Favorite friends first, then restricted users, then by name.
            Items = new ObservableCollection<SelectableKakaoFriendViewModel>(friends.profiles.OrderByDescending(x => x.is_favorite).ThenByDescending(x => x.blocked == true).ThenBy(x => x.display_name).Select(CreateSelectableFriendViewModel));
            UpdateSelectionState();
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    private void SelectRestricted() => SetSelection(Items.Where(x => x.IsRestricted).ToList(), true);

    [RelayCommand]
    private void DeselectFavorite() => SetSelection(Items.Where(x => x.IsFavorite).ToList(), false);

    [RelayCommand]
    private void InvertSelection()
    {
        _isApplyingBulkSelection = true;
        foreach (var item in Items) item.SetSelected(!item.IsSelected);
        _isApplyingBulkSelection = false;

        UpdateSelectionState();
    }

    // Stops the running delete loop after the current friend; the loop reports the partial result.
    [RelayCommand]
    private void CancelDelete() => _cancellationTokenSource?.Cancel();

    // Deletes the selected friends one by one, reporting the progress and the result counts.
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (IsDeleting) return;

        var selectedViewModels = Items.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("안내", "삭제할 친구를 먼저 선택해주세요."));
            return;
        }

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("삭제", $"{selectedViewModels.Count}명의 친구를 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        // The window must stay open until the whole flow (loop, result dialog, and refresh)
        // finishes, so the loop cannot outlive the page it reports to.
        SetWindowCloseBlocked(true, "친구 일괄 삭제가 진행 중입니다. 중단한 뒤 닫아주세요.");
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            IsDeleting = true;
            DeleteTotal = selectedViewModels.Count;
            DeleteProgress = 0;

            var successCount = 0;
            var failureCount = 0;
            var wasCancelled = false;

            try
            {
                for (var index = 0; index < selectedViewModels.Count; index++)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        if (await KakaoStoryApiHandler.DeleteFriend(selectedViewModels[index].UserId)) successCount++;
                        else failureCount++;
                    }
                    catch { failureCount++; }

                    DeleteProgress = index + 1;
                    await Task.Delay(DeleteDelayMilliseconds, token);
                }
            }
            catch (OperationCanceledException) { wasCancelled = true; }
            finally
            {
                IsDeleting = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            // The deleted friends must not be served from the cached friend list.
            CommonShared.KakaoFriends = null;

            // The page may have been navigated away from while the loop was running.
            if (_isLeavingPage) return;

            var failureText = failureCount > 0 ? $", 실패 {failureCount}건" : string.Empty;
            var message = wasCancelled ? $"일괄 삭제가 중간에 취소되었습니다. (성공 {successCount}건{failureText})" : $"삭제가 완료되었습니다. (성공 {successCount}건{failureText})";
            await ShowMessageDialogAsync(new MessageDialogParameters(wasCancelled ? "취소됨" : "완료", message));

            RefreshFriendPages();
            await RefreshAsync();
        }
        finally { SetWindowCloseBlocked(false); }
    }

    // Called when the hosting page is navigated away from: stops the loop and suppresses the
    // result dialog and refresh, which would target a page that is no longer on screen.
    public void OnNavigatedAway()
    {
        _isLeavingPage = true;
        _cancellationTokenSource?.Cancel();
    }

    private SelectableKakaoFriendViewModel CreateSelectableFriendViewModel(FriendData.Profile profile)
    {
        var viewModel = new SelectableKakaoFriendViewModel(profile, this);
        viewModel.SelectionChanged += OnItemSelectionChanged;
        return viewModel;
    }

    private void OnItemSelectionChanged(object sender, EventArgs e)
    {
        if (_isApplyingBulkSelection) return;

        UpdateSelectionState();
    }

    // Applies one programmatic selection change and republishes the summary once instead of
    // once per item.
    private void SetSelection(IReadOnlyList<SelectableKakaoFriendViewModel> viewModels, bool isSelected)
    {
        _isApplyingBulkSelection = true;
        foreach (var viewModel in viewModels) viewModel.SetSelected(isSelected);
        _isApplyingBulkSelection = false;

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

    // Friend changes affect the main window's friendship side bar and Kakao suggestions, so
    // the main window's surfaces are asked to reload instead of receiving per-friend messages.
    private static void RefreshFriendPages() => RefreshRequestedMessage.Send(MainWindow.Frame.XamlRoot);
}
