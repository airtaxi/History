using History.Commons.Enums;

namespace History.Commons.DataTypes.ResponseDtos;

public class InviteCodeRequestResponseDto
{
    public string Id { get; set; }
    public UserResponseDto Requester { get; set; }
    public string Reason { get; set; }
    public int RequestedCount { get; set; }
    public InviteCodeRequestStatus Status { get; set; }
    public string ModeratorMessage { get; set; }
    public int GrantedCount { get; set; }
    public int ActiveCodeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public InviteCodeRequestResponseDto() { }

    public InviteCodeRequestResponseDto(InviteCodeRequest request) : this()
    {
        Id = request.Id;
        Reason = request.Reason;
        RequestedCount = request.RequestedCount;
        Status = request.Status;
        ModeratorMessage = request.ModeratorMessage;
        GrantedCount = request.GrantedCount;
        CreatedAt = request.CreatedAt;
        ProcessedAt = request.ProcessedAt;
    }
}