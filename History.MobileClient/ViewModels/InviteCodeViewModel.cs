using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.ViewModels;

public class InviteCodeViewModel(InviteCodeResponseDto inviteCode)
{
    public InviteCodeResponseDto InviteCode => inviteCode;
    public string Id => inviteCode.Id;
    public string Code => inviteCode.Code;
    public bool IsActive => inviteCode.IsActive;
    public string UsedByNickname => inviteCode.UsedBy?.Nickname ?? "알 수 없음";
    public bool IsUsed => !inviteCode.IsActive && !string.IsNullOrEmpty(inviteCode.UsedByUserId);
    public DateTime? UsedAt => inviteCode.UsedAt;
    public DateTime CreatedAt => inviteCode.CreatedAt;
    public string StatusText => inviteCode.IsActive ? "사용 가능" : "사용됨";
}