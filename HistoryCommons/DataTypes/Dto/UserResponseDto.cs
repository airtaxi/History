using History.Commons.Enums;

namespace History.Commons.DataTypes.Dto;

public class UserResponseDto
{
    public string UserId { get; set; }

    public Rank Rank { get; set; }
    public SocialService SocialService { get; set; }

    public string Nickname { get; set; }
    public DateTime? Birthday { get; set; }
    public string Description { get; set; }

    public string ProfileMediaId { get; set; }
    public string BackgroundMediaId { get; set; }

    public int PostCount { get; set; }
    public int FriendCount { get; set; }
}
