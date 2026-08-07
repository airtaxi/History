using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.InviteCode;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class InviteCodesPage : ContentPage
{
    private readonly ObservableCollection<InviteCodeViewModel> _viewModels = [];
    private bool _areThereNoMoreCodesToLoad;
    private bool _isInForeground;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public InviteCodesPage()
    {
        InitializeComponent();
        InviteCodesCollectionView.ItemsSource = _viewModels;
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChanged);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
        _ = MarkNotificationsAsReadAsync();
        _ = RefreshAsync();
    }

    private async Task MarkNotificationsAsReadAsync()
    {
        var success = await Shared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByInviteCodeRequestResult());
        if (success) WeakReferenceMessenger.Default.Send(new NotificationTypeReadMessage(NotificationType.InviteCodeRequestResult));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChanged(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMoreCodesToLoad = false;
            _viewModels.Clear();
            var result = await App.ExecuteRequestAsync(new GetMyInviteCodes());
            if (result.IsSuccess)
                foreach (var code in result.Value) _viewModels.Add(new InviteCodeViewModel(code));
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount == 0 || _areThereNoMoreCodesToLoad) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;
            var result = await App.ExecuteRequestAsync(new GetMyInviteCodes(lastViewModel.Id));
            if (result.IsSuccess)
            {
                _areThereNoMoreCodesToLoad = result.Value.Count == 0;
                foreach (var code in result.Value) _viewModels.Add(new InviteCodeViewModel(code));
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRequestInviteCodesTapped(object sender, TappedEventArgs e)
    {
        // Moderators and above can generate codes regardless of active code count
        if (Shared.MyRank < Rank.Moderator)
        {
            // Check active code count first; only allow requesting when zero active codes remain
            var countResult = await App.ExecuteRequestAsync(new GetActiveInviteCodeCount());
            if (countResult.IsSuccess && countResult.Value > 0)
            {
                await App.Page.DisplayAlertAsync("안내", $"유효한 초대 코드가 {countResult.Value}개 남아있습니다. 모두 사용한 후에 요청할 수 있습니다.", Constants.PromptOk);
                return;
            }
        }

        await ShowRequestDialogAsync();
    }

    private async Task ShowRequestDialogAsync()
    {
        var countStr = await App.Page.DisplayPromptAsync(
            "초대 코드 요청",
            "요청할 초대 코드 갯수를 입력하세요 (1-50)",
            initialValue: "1",
            maxLength: 2,
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(countStr)) return;
        if (!int.TryParse(countStr, out var count) || count < 1 || count > 50)
        {
            await App.Page.DisplayAlertAsync("오류", "1~50 사이의 숫자를 입력해주세요.", Constants.PromptOk);
            return;
        }

        // Moderators and above generate invite codes immediately instead of submitting a request
        if (Shared.MyRank >= Rank.Moderator)
        {
            var createResult = await App.ExecuteRequestAsync(new CreateInviteCodeByAdmin(Shared.UserId, count), [ErrorType.BadRequest, ErrorType.NotFound]);
            if (createResult.IsSuccess)
            {
                await App.Page.DisplayAlertAsync("안내", "초대 코드가 생성되었습니다.", Constants.PromptOk);
                await RefreshAsync();
            }
            else await App.Page.DisplayAlertAsync("오류", createResult.ErrorMessage, Constants.PromptOk);
            return;
        }

        var reasonStr = await App.Page.DisplayPromptAsync(
            "초대 코드 요청",
            "요청 사유를 입력하세요 (선택사항)",
            maxLength: 500);

        var result = await App.ExecuteRequestAsync(new RequestInviteCodes(reasonStr, count), [ErrorType.BadRequest, ErrorType.Conflict]);
        if (result.IsSuccess)
        {
            await App.Page.DisplayAlertAsync("안내", "초대 코드 요청이 전송되었습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
        else
        {
            await App.Page.DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
        }
    }

    private async void OnCopyButtonTapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable) return;
        if (bindable.BindingContext is not InviteCodeViewModel viewModel || !viewModel.IsActive) return;

        try
        {
            await Clipboard.SetTextAsync(viewModel.Code);
            await Toast.Make("초대 코드가 클립보드에 복사되었습니다.").Show();
        }
        catch { await Toast.Make("초대 코드 복사에 실패했습니다.").Show(); }
    }

    private async void OnBackTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnRefreshViewRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        RefreshView.IsRefreshing = false;
    }
}