using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

public class InviteCodeRequestViewModel(InviteCodeRequestResponseDto request)
{
    public InviteCodeRequestResponseDto Request => request;
    public string Id => request.Id;
    public string RequesterNickname => request.Requester?.Nickname ?? "알 수 없음";
    public string RequesterId => request.Requester?.UserId;
    public string Reason => string.IsNullOrEmpty(request.Reason) ? "사유 없음" : request.Reason;
    public int RequestedCount => request.RequestedCount;
    public int ActiveCodeCount => request.ActiveCodeCount;
    public InviteCodeRequestStatus Status => request.Status;
    public string StatusText => request.Status.ToDisplayString();
    public string ModeratorMessage => request.ModeratorMessage;
    public int GrantedCount => request.GrantedCount;
    public DateTime CreatedAt => request.CreatedAt;
    public DateTime? ProcessedAt => request.ProcessedAt;
    public bool IsPending => request.Status == InviteCodeRequestStatus.Pending;
}