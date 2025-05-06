using History.Commons.Enums;

namespace History.Commons.DataTypes.ResponseDtos;

public class PostReactionDto
{
    public UserResponseDto User { get; set; }
    public PostReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
