using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.InviteCode;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Extras;

// Invite code page view model: first-page loading, infinite scroll pagination and the
// request flow. Moderators and above generate codes immediately; other accounts submit
// a request that an operator reviews later.
public partial class InviteCodesPageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreCodesToLoad;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<InviteCodeViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMoreCodesToLoad = false;

            var codesResult = await ExecuteRequestAsync(new GetMyInviteCodes());

            // Swap the whole collection once so the repeater sees a single reset instead of
            // one incremental change per code.
            if (codesResult.IsSuccess) Items = new ObservableCollection<InviteCodeViewModel>(codesResult.Value.Select(x => new InviteCodeViewModel(x)));
            else Items = [];
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreCodesToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var codesResult = await ExecuteRequestAsync(new GetMyInviteCodes(lastViewModel.Id));
            if (codesResult.IsSuccess)
            {
                var viewModels = codesResult.Value.Select(x => new InviteCodeViewModel(x)).ToList();

                // One reset per fetched page instead of one incremental change per code.
                if (viewModels.Count > 0) Items = new ObservableCollection<InviteCodeViewModel>([.. Items, .. viewModels]);

                if (codesResult.Value.Count == 0) _areThereNoMoreCodesToLoad = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Opening the page means the user has seen their request results, so the matching
    // notifications are cleared on the server and on the notification surfaces.
    public async Task MarkNotificationsAsReadAsync()
    {
        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByInviteCodeRequestResult());
        if (success) WeakReferenceMessenger.Default.Send(new NotificationTypeReadMessage(NotificationType.InviteCodeRequestResult));
    }

    public async Task RequestInviteCodesAsync()
    {
        // Moderators and above can generate codes regardless of the remaining active code count.
        if (CommonShared.MyRank < Rank.Moderator)
        {
            // Only accounts with no active codes left can submit a request.
            var countResult = await ExecuteRequestAsync(new GetActiveInviteCodeCount());
            if (countResult.IsSuccess && countResult.Value > 0)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters("안내", $"유효한 초대 코드가 {countResult.Value}개 남아있습니다. 모두 사용한 후에 요청할 수 있습니다."));
                return;
            }
        }

        var countText = await ShowInputDialogAsync(new InputDialogParameters("초대 코드 요청", "요청할 초대 코드 갯수를 입력하세요 (1-50)", defaultText: "1", showCancel: true, numberOnly: true, maxLength: 2));
        if (string.IsNullOrEmpty(countText)) return;
        if (!int.TryParse(countText, out var count) || count < 1 || count > 50)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "1~50 사이의 숫자를 입력해주세요."));
            return;
        }

        // Moderators and above generate the codes immediately instead of submitting a request.
        if (CommonShared.MyRank >= Rank.Moderator)
        {
            var createResult = await ExecuteRequestAsync(new CreateInviteCodeByAdmin(CommonShared.UserId, count), ErrorType.BadRequest, ErrorType.NotFound);
            if (createResult.IsSuccess)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters("안내", "초대 코드가 생성되었습니다."));
                await RefreshAsync();
            }
            else if (createResult.Error is ErrorType.BadRequest or ErrorType.NotFound) await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, createResult.ErrorMessage));
            return;
        }

        var reason = await ShowInputDialogAsync(new InputDialogParameters("초대 코드 요청", "요청 사유를 입력하세요 (선택사항)", showCancel: true, maxLength: 500));

        var requestResult = await ExecuteRequestAsync(new RequestInviteCodes(reason, count), ErrorType.BadRequest, ErrorType.Conflict);
        if (requestResult.IsSuccess)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("안내", "초대 코드 요청이 전송되었습니다."));
            await RefreshAsync();
        }
        else if (requestResult.Error is ErrorType.BadRequest or ErrorType.Conflict) await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, requestResult.ErrorMessage));
    }
}
