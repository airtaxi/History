using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.Commons.DataTypes.ResponseDtos;

public class PostResponseDto
{
    public string Id { get; set; }

    public UserResponseDto User { get; set; }
    public bool IsRepost { get; set; }

    public DiscoveryOption DiscoveryOption { get; set; }
    public List<string> DiscoveryOptionSelectedUserIds { get; set; }

    public List<BaseContent> Contents { get; set; } = [];
    public PostResponseDto ParentPost { get; set; }

    public int CommentsCount { get; set; }
    public List<CommentResponseDto> Comments { get; set; } = [];

    public List<SharedAndRepostedUserDto> SharedAndRepostedUsers { get; set; } = [];
    public List<PostReactionDto> PostReactions { get; set; } = [];

    public bool DisallowShare { get; set; }
    public AccessPermission? CommentPermission { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public List<string> Hashtags { get; set; } = [];

    public bool IsBookmarked { get; set; }
}
