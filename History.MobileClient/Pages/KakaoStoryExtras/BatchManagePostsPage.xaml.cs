using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.Pages;

public partial class BatchManagePostsPage : ContentPage
{
    private readonly bool _isDeleteMode;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _runLoopTask;
    private bool _isInForeground;
    private bool _isRunning;
    private string _activeFilter = "all";

    public BatchManagePostsPage(bool isDeleteMode)
    {
        _isDeleteMode = isDeleteMode;
        InitializeComponent();

        TitleLabel.Text = isDeleteMode ? "스토리 일괄 삭제" : "스토리 일괄 변경";
        ExecuteButton.Text = isDeleteMode ? "삭제" : "변경";

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void UpdateFilterButtonVisuals()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color ?? Colors.Orange;
        var isDarkTheme = Utils.GetGlobalAppTheme() == AppTheme.Dark;
        var inactiveBackgroundColor = isDarkTheme ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xEA, 0xEA, 0xEA);
        var inactiveTextColor = isDarkTheme ? Color.FromRgb(0xAA, 0xAA, 0xAA) : Color.FromRgb(0x66, 0x66, 0x66);

        SetFilterButtonVisual(AllFilterButton, _activeFilter == "all", primaryColor, inactiveBackgroundColor, inactiveTextColor);
        SetFilterButtonVisual(PublicFilterButton, _activeFilter == "public", primaryColor, inactiveBackgroundColor, inactiveTextColor);
        SetFilterButtonVisual(FriendsFilterButton, _activeFilter == "friends", primaryColor, inactiveBackgroundColor, inactiveTextColor);
        SetFilterButtonVisual(OnlyMeFilterButton, _activeFilter == "onlyMe", primaryColor, inactiveBackgroundColor, inactiveTextColor);
        SetFilterButtonVisual(BlindedFilterButton, _activeFilter == "blinded", primaryColor, inactiveBackgroundColor, inactiveTextColor);
    }

    private static void SetFilterButtonVisual(Button button, bool isActive, Color primaryColor, Color inactiveBackgroundColor, Color inactiveTextColor)
    {
        button.BackgroundColor = isActive ? primaryColor : inactiveBackgroundColor;
        button.TextColor = isActive ? Colors.White : inactiveTextColor;
    }

    private void SetFilter(string filter)
    {
        _activeFilter = filter;
        UpdateFilterButtonVisuals();
    }

    private void OnAllFilterButtonClicked(object sender, EventArgs e) => SetFilter("all");
    private void OnPublicFilterButtonClicked(object sender, EventArgs e) => SetFilter("public");
    private void OnFriendsFilterButtonClicked(object sender, EventArgs e) => SetFilter("friends");
    private void OnOnlyMeFilterButtonClicked(object sender, EventArgs e) => SetFilter("onlyMe");
    private void OnBlindedFilterButtonClicked(object sender, EventArgs e) => SetFilter("blinded");

    private void OnExcludeBookmarkedLabelTapped(object sender, TappedEventArgs e) => ExcludeBookmarkedCheckBox.IsChecked = !ExcludeBookmarkedCheckBox.IsChecked;

    private bool IsTargetPost(PostData postData)
    {
        // Only own posts are targeted (the iterated feed is the user's own profile).
        if (postData.actor?.id != Shared.KakaoUserId) return false;

        if (_activeFilter == "public" && postData.permission != "A") return false;
        else if (_activeFilter == "friends" && postData.permission != "F") return false;
        else if (_activeFilter == "onlyMe" && postData.permission != "M") return false;
        else if (_activeFilter == "blinded" && !postData.blinded) return false;

        if (ExcludeBookmarkedCheckBox.IsChecked == true && postData.bookmarked) return false;

        return true;
    }

    private async void OnExecuteButtonClicked(object sender, EventArgs e)
    {
        if (_isRunning) return;

        if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        var userId = Shared.KakaoUserId;
        if (userId == null)
        {
            await DisplayAlertAsync("오류", "카카오스토리 사용자 정보를 불러오지 못했습니다.", Constants.PromptOk);
            return;
        }

        var filterDisplay = _activeFilter switch
        {
            "public" => "전체 공개",
            "friends" => "친구 공개",
            "onlyMe" => "나만 보기",
            "blinded" => "제한된 게시글",
            _ => "모든 게시글"
        };
        if (ExcludeBookmarkedCheckBox.IsChecked == true) filterDisplay += " (관심글 제외)";

        if (_isDeleteMode)
        {
            var confirm = await DisplayAlertAsync("삭제", $"'{filterDisplay}'에 해당하는 게시글을 전부 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", Constants.PromptCancel);
            if (!confirm) return;

            if (_isRunning) return; // Re-check guard after async gaps to prevent concurrent loops.
            _runLoopTask = RunLoopAsync(userId, isDelete: true);
            await _runLoopTask;
        }
        else
        {
            var options = new List<string>
            {
                "전체 공개",
                "친구 공개",
                "나만 보기"
            };
            var action = await DisplayActionSheetAsync("변경할 공개 범위 선택", Constants.PromptCancel, null, [.. options]);
            if (action == null || action == Constants.PromptCancel) return;

            var permission = action switch
            {
                "전체 공개" => "A",
                "친구 공개" => "F",
                "나만 보기" => "M",
                _ => null
            };
            if (permission == null) return;

            var confirm = await DisplayAlertAsync("변경", $"공개 범위가 '{filterDisplay}'인 게시글을 '{action}'로 일괄 변경하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (!confirm) return;

            if (_isRunning) return; // Re-check guard after async gaps to prevent concurrent loops.
            _runLoopTask = RunLoopAsync(userId, isDelete: false, permission);
            await _runLoopTask;
        }
    }

    private async Task RunLoopAsync(string userId, bool isDelete, string permission = null)
    {
        var token = _cancellationTokenSource.Token;
        _isRunning = true;
        MainActivityIndicator.IsRunning = true;
        SetControlsEnabled(false);
        ScanProgressBar.IsVisible = true;
        ProgressLabel.IsVisible = true;

        var successCount = 0;
        var failureCount = 0;
        var scannedCount = 0;
        var wasCancelled = false;
        var hadError = false;
        try
        {
            // Fetch the total activity count from the profile highlight so the
            // progress bar can show the overall scan progress.
            var totalCount = 0;
            try
            {
                var highlight = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileHighlight(userId));
                totalCount = highlight?.highlight?.FirstOrDefault(x => x.type == "counts")?.@object?.activity_count ?? 0;
            }
            catch { }

            var from = (string)null;
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var profileObject = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(userId, from));
                if (profileObject?.activities == null) break;

                var activities = profileObject.activities;
                foreach (var activity in activities)
                {
                    token.ThrowIfCancellationRequested();

                    scannedCount++;
                    UpdateProgress(scannedCount, totalCount, successCount, failureCount);

                    if (!IsTargetPost(activity)) continue;

                    try
                    {
                        var isSuccess = isDelete
                            ? await KakaoStoryApiHandler.DeletePost(activity.id)
                            : await KakaoStoryApiHandler.SetActivityProfile(activity.id, permission, activity.sharable, activity.comment_all_writable, activity.is_must_read);
                        if (isSuccess) successCount++;
                        else failureCount++;
                    }
                    catch { failureCount++; }

                    UpdateProgress(scannedCount, totalCount, successCount, failureCount);
                    await Task.Delay(100, token);
                }

                // The profile feed has no next_since; the cursor is the last activity id,
                // advanced only while more than 15 items are returned (Kakao Story Manager Plus pattern).
                if (activities.Count <= 15) break;
                from = activities.LastOrDefault()?.id;
                if (from == null) break;
            }
        }
        catch (OperationCanceledException) { wasCancelled = true; }
        catch (Exception) { hadError = true; }
        finally
        {
            _isRunning = false;
            MainActivityIndicator.IsRunning = false;
            SetControlsEnabled(true);
            ScanProgressBar.IsVisible = false;
            ProgressLabel.IsVisible = false;
        }

        if (successCount > 0) InvalidatePostPages();

        var actionName = isDelete ? "삭제" : "변경";

        // The page may have been popped by a non-back route (iOS swipe, shell
        // navigation); skip alerts once it is no longer on screen.
        if (!_isInForeground) return;

        if (hadError)
        {
            await DisplayAlertAsync("오류", $"일괄 {actionName}{Utils.GetSubjectParticle(actionName)} 중 오류가 발생했습니다. (성공 {successCount}건, 실패 {failureCount}건)", Constants.PromptOk);
            return;
        }

        if (wasCancelled)
        {
            await DisplayAlertAsync("취소됨", $"일괄 {actionName}{Utils.GetSubjectParticle(actionName)} 중간에 취소되었습니다. (성공 {successCount}건, 실패 {failureCount}건)", Constants.PromptOk);
            return;
        }

        var message = $"일괄 {actionName}{Utils.GetSubjectParticle(actionName)} 완료되었습니다. (성공 {successCount}건";
        if (failureCount > 0) message += $", 실패 {failureCount}건";
        message += ")";
        await DisplayAlertAsync("완료", message, Constants.PromptOk);
    }

    private void UpdateProgress(int scannedCount, int totalCount, int successCount, int failureCount)
    {
        if (totalCount > 0) ScanProgressBar.Progress = Math.Min(1.0, (double)scannedCount / totalCount);
        ProgressLabel.Text = $"조회 {scannedCount}/{totalCount}건 / 처리 {successCount + failureCount}건";
    }

    // Keeps the back image tappable while the batch loop is running so the user
    // can cancel it; only the filter/execute controls are disabled.
    private void SetControlsEnabled(bool isEnabled)
    {
        ExecuteButton.IsEnabled = isEnabled;
        AllFilterButton.IsEnabled = isEnabled;
        PublicFilterButton.IsEnabled = isEnabled;
        FriendsFilterButton.IsEnabled = isEnabled;
        OnlyMeFilterButton.IsEnabled = isEnabled;
        BlindedFilterButton.IsEnabled = isEnabled;
        ExcludeBookmarkedCheckBox.IsEnabled = isEnabled;
    }

    private static void InvalidatePostPages()
    {
        TimelinePage.ShouldRefreshKakaoStory = true;
        UserPage.ShouldRefreshKakaoStory = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
        if (!_isRunning) _cancellationTokenSource = new CancellationTokenSource();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        UpdateFilterButtonVisuals();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
        _cancellationTokenSource?.Cancel();
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        // The batch loop manages its own controls/indicator state; ignore
        // per-request loading messages to avoid interference with the loop UI.
        if (_isRunning) return;

        var isLoading = message.Value;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            SetControlsEnabled(!isLoading);
        });
    }

    protected override bool OnBackButtonPressed()
    {
        _ = HandleBackAsync();
        return true;
    }

    private async Task HandleBackAsync()
    {
        if (!_isRunning)
        {
            await App.PopAsync();
            return;
        }

        var cancel = await DisplayAlertAsync("취소", "일괄 작업을 중단하시겠습니까?", "중단", Constants.PromptCancel);
        if (!cancel) return;

        _cancellationTokenSource?.Cancel();
        if (_runLoopTask != null) await _runLoopTask;
        await App.PopAsync();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await HandleBackAsync();
}
