using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Post
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Discovery option for post.
    /// </summary>
    public DiscoveryOption DiscoveryOption { get; set; }

    /// <summary>
    /// Selected user ids if discovery option is selected users.
    /// </summary>
    public List<string> DiscoveryOptionSelectedUserIds { get; set; } = [];

    /// <summary>
    /// A user id who created this post.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// If this true, this post is repost. Means just share other post without contents.
    /// Only takes effect for user interface.
    /// </summary>
    public bool IsRepost { get; set; }

    /// <summary>
    /// Contents of post.
    /// </summary>
    public List<BaseContent> Contents { get; set; } = [];

    /// <summary>
    /// Parent post id. Not null means this post shares other post.
    /// </summary>
    public string ParentPostId { get; set; }

    /// <summary>
    /// Created at time of post.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modified at time of post.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// If true, the post will be displayed on the seperate menu
    /// </summary>
    public bool IsPublicPost { get; set; }

    /// <summary>
    /// The data used when searching for this post.
    /// </summary>
    public string SearchIndex { get; set; } = string.Empty;

    /// <summary>
    /// If true, this post cannot be shared by other users.
    /// </summary>
    public bool DisallowShare { get; set; }

    /// <summary>
    /// Comment permission for this post.
    /// </summary>
    public AccessPermission? CommentPermission { get; set; }

    /// <summary>
    /// Hashtags for this post.
    /// </summary>
    public List<string> Hashtags { get; set; } = [];
}
