using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class BatchDeleteFriendsPage : ContentPage
{
    private readonly ObservableCollection<SelectableKakaoFriendViewModel> _viewModels = [];
    private CancellationTokenSource _cancellationTokenSource;
    private Task _deleteTask;
    private bool _isInForeground;
    private bool _isSelectAll;
    private bool _isDeleting;

    public BatchDeleteFriendsPage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<KakaoFriendSelectionChangedMessage>(this, OnFriendSelectionChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnFriendSelectionChangedMessageReceived(object recipient, KakaoFriendSelectionChangedMessage message) => UpdateDeleteLabel();

    private void UpdateDeleteLabel()
    {
        if (_isDeleting) return;
        var selectedCount = _viewModels.Count(x => x.IsSelected);
        DeleteLabel.IsVisible = selectedCount > 0;
        DeleteLabel.Text = $"삭제 ({selectedCount})";
    }

    private async Task RefreshAsync()
    {
        if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        try
        {
            var friends = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFriends());
            if (friends?.profiles == null)
            {
                await DisplayAlertAsync("오류", "카카오스토리 친구 목록을 불러오지 못했습니다.", Constants.PromptOk);
                return;
            }

            _isSelectAll = false;
            SelectAllButton.Text = "전체 선택";
            _viewModels.Clear();
            // Sort: favorite friends first (descending), then blocked users (descending), then by name (ascending).
            foreach (var profile in friends.profiles.OrderByDescending(x => x.is_favorite).ThenByDescending(x => x.blocked == true).ThenBy(x => x.display_name)) _viewModels.Add(new SelectableKakaoFriendViewModel(profile));

            EmptyLabel.IsVisible = _viewModels.Count == 0;
            UpdateDeleteLabel();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("오류", $"카카오스토리 친구 목록을 불러오지 못했습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }

    private async void OnDeleteLabelTapped(object sender, TappedEventArgs e)
    {
        if (_isDeleting) return;

        var selectedViewModels = _viewModels.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await DisplayAlertAsync("안내", "삭제할 친구를 먼저 선택해주세요.", Constants.PromptOk);
            return;
        }

        var confirm = await DisplayAlertAsync("삭제", $"{selectedViewModels.Count}명의 친구를 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", Constants.PromptCancel);
        if (!confirm) return;

        if (_isDeleting) return; // Re-check guard after async gaps to prevent concurrent loops.
        _deleteTask = DeleteFriendsAsync(selectedViewModels);
        await _deleteTask;
    }

    private async Task DeleteFriendsAsync(List<SelectableKakaoFriendViewModel> selectedViewModels)
    {
        var token = _cancellationTokenSource.Token;
        _isDeleting = true;
        MainActivityIndicator.IsRunning = true;
        SetControlsEnabled(false);
        ProgressBar.IsVisible = true;
        ProgressLabel.IsVisible = true;

        var successCount = 0;
        var failureCount = 0;
        var wasCancelled = false;
        var totalCount = selectedViewModels.Count;
        try
        {
            for (var index = 0; index < totalCount; index++)
            {
                token.ThrowIfCancellationRequested();

                var viewModel = selectedViewModels[index];
                try
                {
                    if (await KakaoStoryApiHandler.DeleteFriend(viewModel.Id)) successCount++;
                    else failureCount++;
                }
                catch { failureCount++; }

                ProgressBar.Progress = (double)(index + 1) / totalCount;
                ProgressLabel.Text = $"{index + 1}/{totalCount}";
                await Task.Delay(100, token);
            }
        }
        catch (OperationCanceledException) { wasCancelled = true; }
        finally
        {
            _isDeleting = false;
            MainActivityIndicator.IsRunning = false;
            SetControlsEnabled(true);
            ProgressBar.IsVisible = false;
            ProgressLabel.IsVisible = false;
        }

        Shared.KakaoFriends = null;

        // The page may have been popped by a non-back route (iOS swipe, shell
        // navigation); skip alerts/refresh once it is no longer on screen.
        if (!_isInForeground) return;

        if (wasCancelled)
        {
            await DisplayAlertAsync("취소됨", $"일괄 삭제가 중간에 취소되었습니다. (성공 {successCount}건, 실패 {failureCount}건)", Constants.PromptOk);
            if (_isInForeground) await RefreshAsync();
            return;
        }

        var message = $"삭제가 완료되었습니다. (성공 {successCount}건";
        if (failureCount > 0) message += $", 실패 {failureCount}건";
        message += ")";
        await DisplayAlertAsync("완료", message, Constants.PromptOk);

        await RefreshAsync();
    }

    private void OnSelectAllButtonClicked(object sender, EventArgs e)
    {
        var shouldSelectAll = !_isSelectAll;
        _isSelectAll = shouldSelectAll;
        SelectAllButton.Text = shouldSelectAll ? "전체 해제" : "전체 선택";
        foreach (var viewModel in _viewModels) viewModel.SetSelected(shouldSelectAll, notify: false);
        UpdateDeleteLabel();
    }

    private void OnSelectBlockedButtonClicked(object sender, EventArgs e)
    {
        foreach (var viewModel in _viewModels.Where(x => x.IsBlocked)) viewModel.SetSelected(true, notify: false);
        UpdateDeleteLabel();
    }

    private void OnDeselectFavoriteButtonClicked(object sender, EventArgs e)
    {
        foreach (var viewModel in _viewModels.Where(x => x.IsFavorite)) viewModel.SetSelected(false, notify: false);
        UpdateDeleteLabel();
    }

    private void OnInvertSelectionButtonClicked(object sender, EventArgs e)
    {
        foreach (var viewModel in _viewModels) viewModel.SetSelected(!viewModel.IsSelected, notify: false);
        UpdateDeleteLabel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
        if (!_isDeleting) _cancellationTokenSource = new CancellationTokenSource();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        await RefreshAsync();
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
        if (_isDeleting) return;

        var isLoading = message.Value;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            SetControlsEnabled(!isLoading);
        });
    }

    // Keeps the back image tappable while the batch loop is running so the user
    // can cancel it; only the select/delete controls are disabled.
    private void SetControlsEnabled(bool isEnabled)
    {
        if (isEnabled) UpdateDeleteLabel();
        else DeleteLabel.IsVisible = false;

        SelectAllButton.IsEnabled = isEnabled;
        SelectBlockedButton.IsEnabled = isEnabled;
        DeselectFavoriteButton.IsEnabled = isEnabled;
        InvertSelectionButton.IsEnabled = isEnabled;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = HandleBackAsync();
        return true;
    }

    private async Task HandleBackAsync()
    {
        if (!_isDeleting)
        {
            await App.PopAsync();
            return;
        }

        var cancel = await DisplayAlertAsync("취소", "일괄 삭제를 중단하시겠습니까?", "중단", Constants.PromptCancel);
        if (!cancel) return;

        _cancellationTokenSource?.Cancel();
        if (_deleteTask != null) await _deleteTask;
        await App.PopAsync();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await HandleBackAsync();
}
