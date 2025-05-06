using History.Commons.DataTypes.Contents;

namespace History.Commons.DataTypes.ResponseDtos;

public class PostResponseDto
{
    public string Id { get; set; }

    public UserResponseDto User { get; set; }
    public bool IsRepost { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public PostResponseDto ParentPost { get; set; }
    
    public bool HasBeenSimpleReposted { get; set; }

    public int CommentsCount { get; set; }
    public List<CommentResponseDto> Comments { get; set; } = [];

    public List<PostReactionDto> PostReactions { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
