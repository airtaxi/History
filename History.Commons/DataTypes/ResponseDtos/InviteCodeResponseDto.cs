using History.Commons.DataTypes.ResponseDtos;

namespace History.Commons.DataTypes.ResponseDtos;

public class InviteCodeResponseDto
{
    public string Id { get; set; }
    public string Code { get; set; }
    public string OwnerId { get; set; }
    public bool IsActive { get; set; }
    public string UsedByUserId { get; set; }
    public UserResponseDto UsedBy { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public InviteCodeResponseDto() { }

    public InviteCodeResponseDto(InviteCode code) : this()
    {
        Id = code.Id;
        Code = code.Code;
        OwnerId = code.OwnerId;
        IsActive = code.IsActive;
        UsedByUserId = code.UsedByUserId;
        UsedAt = code.UsedAt;
        CreatedAt = code.CreatedAt;
    }
}