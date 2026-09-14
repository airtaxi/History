using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels.Extras;

// Wraps an invite code request entry (requester + review state) and owns the row's review
// commands, which run on the page view model's dialog and API surface.
public sealed partial class InviteCodeRequestViewModel(InviteCodeRequestResponseDto request, InviteCodeRequestsPageViewModel pageViewModel) : ObservableObject
{
    private const string UnknownNickname = "알 수 없음";

    public string Id => request.Id;
    public string RequesterNickname => request.Requester?.Nickname ?? UnknownNickname;
    public int RequestedCount => request.RequestedCount;
    public bool IsPending => request.Status == InviteCodeRequestStatus.Pending;
    public string StatusText => request.Status.ToDisplayString();
    public string Reason => string.IsNullOrEmpty(request.Reason) ? "사유 없음" : request.Reason;

    public string RequestCountText => $"요청 갯수: {request.RequestedCount}개";
    public string ActiveCodeCountText => $"보유 중인 유효 코드: {request.ActiveCodeCount}개";
    public string CreatedAtText => $"요청 시간: {request.CreatedAt.ToLocalTime():yyyy.MM.dd HH:mm}";
    public string ModeratorMessageText => $"관리자 메시지: {(string.IsNullOrEmpty(request.ModeratorMessage) ? "없음" : request.ModeratorMessage)}";

    [RelayCommand]
    private async Task AcceptAsync() => await pageViewModel.AcceptAsync(this);

    [RelayCommand]
    private async Task RejectAsync() => await pageViewModel.RejectAsync(this);
}
