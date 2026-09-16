using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels.Extras;

// Story batch management state: filters the signed-in account's own Kakao Story posts and
// processes the matching ones one by one (the API has no bulk endpoint) with progress and
// cancellation. The page mode decides between deletion and permission retargeting.
public sealed partial class BatchManageKakaoPostsPageViewModel : BaseViewModel
{
    private const int ProcessDelayMilliseconds = 100;

    // The profile feed only advances its cursor while a full Kakao page was returned.
    private const int ProfileFeedPageSize = 15;

    private CancellationTokenSource _cancellationTokenSource;
    private bool _isLeavingPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExecuteButtonText))]
    public partial KakaoStoryBatchManageMode Mode { get; private set; }

    [ObservableProperty]
    public partial KakaoStoryPostFilter SelectedFilter { get; set; } = KakaoStoryPostFilter.All;

    [ObservableProperty]
    public partial bool ExcludeBookmarked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    public partial double ProgressValue { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    public partial double ProgressMaximum { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    public partial int ProcessedCount { get; private set; }

    public bool IsNotRunning => !IsRunning;

    public bool IsDeleteMode => Mode == KakaoStoryBatchManageMode.Delete;

    public string ExecuteButtonText => IsDeleteMode ? "삭제" : "변경";

    public string ProgressText => $"조회 {ProgressValue:0}/{ProgressMaximum:0}건 / 처리 {ProcessedCount}건";

    // Stores the requested mode; XamlRoot-independent, so it can run from OnNavigatedTo.
    public void Initialize(KakaoStoryBatchManageMode mode) => Mode = mode;

    // Cancels the running scan after the current post; the loop reports the partial result.
    [RelayCommand]
    private void Cancel() => _cancellationTokenSource?.Cancel();

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (IsRunning) return;

        if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        var userId = CommonShared.KakaoUserId;
        if (userId == null)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "카카오스토리 사용자 정보를 불러오지 못했습니다."));
            return;
        }

        var permission = (string)null;
        if (IsDeleteMode)
        {
            var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("삭제", $"'{GetFilterDisplayText()}'에 해당하는 게시글을 전부 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", DialogHelper.DefaultCancelButtonText));
            if (confirm != ContentDialogResult.Primary) return;
        }
        else
        {
            var target = await ShowSelectionDialogAsync("변경할 공개 범위 선택", [DiscoveryOption.Everyone.ToDisplayString(), DiscoveryOption.Friends.ToDisplayString(), DiscoveryOption.OnlyMe.ToDisplayString()]);
            if (target == null) return;

            permission = KakaoStoryUtils.MapDiscoveryOptionToKakaoPermission(DiscoveryOptionExtensions.FromDisplayString(target));

            var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("변경", $"공개 범위가 '{GetFilterDisplayText()}'인 게시글을 '{target}'로 일괄 변경하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (confirm != ContentDialogResult.Primary) return;
        }

        // Re-check after the dialog gaps so a second run cannot start concurrently.
        if (IsRunning) return;

        await RunAsync(userId, permission);
    }

    // Scans the signed-in account's own profile feed and processes every post that matches
    // the filter, updating the progress and honoring the cancellation token.
    private async Task RunAsync(string userId, string permission)
    {
        var actionName = IsDeleteMode ? "삭제" : "변경";
        var actionNameWithParticle = $"{actionName}{Utils.GetSubjectParticle(actionName)}";

        // The window must stay open until the whole flow (scan and result dialog) finishes, so
        // the loop cannot outlive the page it reports to.
        SetWindowCloseBlocked(true, $"일괄 {actionNameWithParticle} 진행 중입니다. 중단한 뒤 닫아주세요.");
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            IsRunning = true;
            ProgressValue = 0;
            ProgressMaximum = 0;
            ProcessedCount = 0;

            var successCount = 0;
            var failureCount = 0;
            var scannedCount = 0;
            var wasCancelled = false;
            var hadError = false;

            try
            {
                try
                {
                    var highlight = await KakaoStoryApiHandler.GetProfileHighlight(userId);
                    ProgressMaximum = highlight?.highlight?.FirstOrDefault(x => x.type == "counts")?.@object?.activity_count ?? 0;
                }
                catch { }

                var from = (string)null;
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var profileObject = await KakaoStoryApiHandler.GetProfileFeed(userId, from);
                    if (profileObject?.activities == null) break;

                    var activities = profileObject.activities;
                    foreach (var activity in activities)
                    {
                        token.ThrowIfCancellationRequested();

                        ProgressValue = ++scannedCount;

                        if (!IsTargetPost(activity)) continue;

                        try
                        {
                            var isSuccess = IsDeleteMode ? await KakaoStoryApiHandler.DeletePost(activity.id) : await KakaoStoryApiHandler.SetActivityProfile(activity.id, permission, activity.sharable, activity.comment_all_writable, activity.is_must_read);
                            if (isSuccess) successCount++;
                            else failureCount++;
                        }
                        catch { failureCount++; }

                        ProcessedCount = successCount + failureCount;
                        await Task.Delay(ProcessDelayMilliseconds, token);
                    }

                    // The cursor advances only while a full page came back; the last activity id
                    // is the cursor for the next page.
                    if (activities.Count <= ProfileFeedPageSize) break;
                    from = activities.LastOrDefault()?.id;
                    if (from == null) break;
                }
            }
            catch (OperationCanceledException) { wasCancelled = true; }
            catch (Exception) { hadError = true; }
            finally
            {
                IsRunning = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            // The page may have been navigated away from while the loop was running, so the
            // result dialog and refresh are skipped.
            if (_isLeavingPage) return;

            if (successCount > 0) RefreshPostPages();

            if (hadError)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"일괄 {actionName} 중 오류가 발생했습니다. (성공 {successCount}건, 실패 {failureCount}건)"));
                return;
            }

            var failureText = failureCount > 0 ? $", 실패 {failureCount}건" : string.Empty;
            var message = wasCancelled ? $"일괄 {actionNameWithParticle} 중간에 취소되었습니다. (성공 {successCount}건{failureText})" : $"일괄 {actionNameWithParticle} 완료되었습니다. (성공 {successCount}건{failureText})";
            await ShowMessageDialogAsync(new MessageDialogParameters(wasCancelled ? "취소됨" : "완료", message));
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

    // Only own posts are targeted (the scanned feed is the user's own profile); the filter
    // then narrows by permission, the restricted flag, and the bookmark exclusion.
    private bool IsTargetPost(PostData postData)
    {
        if (postData.actor?.id != CommonShared.KakaoUserId) return false;

        if (SelectedFilter == KakaoStoryPostFilter.Public && postData.permission != "A") return false;
        else if (SelectedFilter == KakaoStoryPostFilter.Friends && postData.permission != "F") return false;
        else if (SelectedFilter == KakaoStoryPostFilter.OnlyMe && postData.permission != "M") return false;
        else if (SelectedFilter == KakaoStoryPostFilter.Blinded && !postData.blinded) return false;

        if (ExcludeBookmarked && postData.bookmarked) return false;

        return true;
    }

    private string GetFilterDisplayText() => SelectedFilter switch
    {
        KakaoStoryPostFilter.Public => "전체 공개",
        KakaoStoryPostFilter.Friends => "친구 공개",
        KakaoStoryPostFilter.OnlyMe => "나만 보기",
        KakaoStoryPostFilter.Blinded => "제한된 게시글",
        _ => "모든 게시글"
    };

    // Post operations replace or remove many posts at once, so the main window's post
    // surfaces are asked to reload instead of receiving per-post change messages.
    private static void RefreshPostPages() => RefreshRequestedMessage.Send(MainWindow.Frame.XamlRoot);
}
