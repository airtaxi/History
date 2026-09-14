using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.InviteCode;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Extras;

// Invite code request review page view model: first-page loading, infinite scroll
// pagination, and the accept/reject review flow for moderators.
public partial class InviteCodeRequestsPageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreRequestsToLoad;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<InviteCodeRequestViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMoreRequestsToLoad = false;

            var requestsResult = await ExecuteRequestAsync(new GetInviteCodeRequests());

            // Swap the whole collection once so the repeater sees a single reset instead of
            // one incremental change per request.
            if (requestsResult.IsSuccess) Items = new ObservableCollection<InviteCodeRequestViewModel>(requestsResult.Value.Select(x => new InviteCodeRequestViewModel(x, this)));
            else Items = [];
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreRequestsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var requestsResult = await ExecuteRequestAsync(new GetInviteCodeRequests(lastViewModel.Id));
            if (requestsResult.IsSuccess)
            {
                var viewModels = requestsResult.Value.Select(x => new InviteCodeRequestViewModel(x, this)).ToList();

                // One reset per fetched page instead of one incremental change per request.
                if (viewModels.Count > 0) Items = new ObservableCollection<InviteCodeRequestViewModel>([.. Items, .. viewModels]);

                if (requestsResult.Value.Count == 0) _areThereNoMoreRequestsToLoad = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Opening the page means the moderator has seen the incoming requests, so the matching
    // notifications are cleared on the server and on the notification surfaces.
    public async Task MarkNotificationsAsReadAsync()
    {
        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByInviteCodeRequest());
        if (success) WeakReferenceMessenger.Default.Send(new NotificationTypeReadMessage(NotificationType.InviteCodeRequest));
    }

    // Reviews a pending request: collects the optional moderator message, confirms, then
    // accepts it (the server issues the requested codes).
    public async Task AcceptAsync(InviteCodeRequestViewModel inviteCodeRequest)
    {
        var message = await ShowInputDialogAsync(new InputDialogParameters("수락", "추가 메시지를 입력하세요 (선택사항)", showCancel: true, maxLength: 500));

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", $"요청을 수락하시겠습니까? {inviteCodeRequest.RequestedCount}개의 초대 코드가 자동 발급됩니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new AcceptInviteCodeRequest(inviteCodeRequest.Id, message), ErrorType.Conflict);
        if (result.IsSuccess)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "초대 코드 요청을 수락했습니다."));
            await RefreshAsync();
        }
        else if (result.Error is ErrorType.Conflict) await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, result.ErrorMessage));
    }

    // Reviews a pending request: collects the optional moderator message, confirms, then
    // rejects it.
    public async Task RejectAsync(InviteCodeRequestViewModel inviteCodeRequest)
    {
        var message = await ShowInputDialogAsync(new InputDialogParameters("거부", "추가 메시지를 입력하세요 (선택사항)", showCancel: true, maxLength: 500));

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", "요청을 거부하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new RejectInviteCodeRequest(inviteCodeRequest.Id, message), ErrorType.Conflict);
        if (result.IsSuccess)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "초대 코드 요청을 거부했습니다."));
            await RefreshAsync();
        }
        else if (result.Error is ErrorType.Conflict) await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, result.ErrorMessage));
    }
}
