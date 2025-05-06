using History.Commons.DataTypes.Contents;

namespace History.Commons.DataTypes.ResponseDtos;

public class CommentResponseDto
{
    public string Id { get; set; }

    public UserResponseDto User { get; set; }

    public List<BaseContent> Contents { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public List<UserResponseDto> LikedUsers { get; set; }
}
