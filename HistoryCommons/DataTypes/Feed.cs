using History.Commons.DataTypes.Content;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Feed
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Discovery option for feed.
    /// </summary>
    public DiscoveryOption DiscoveryOption { get; set; }

    /// <summary>
    /// A user id who created this feed.
    /// </summary>
    public string AuthorUserId { get; set; }

    /// <summary>
    /// If this true, this feed is repost. Means just share other feed without contents.
    /// Only takes effect for user interface.
    /// </summary>
    public bool IsRepost { get; set; }

    /// <summary>
    /// Contents of feed.
    /// </summary>
    public List<BaseContent> Contents { get; set; } = [];

    /// <summary>
    /// Parent feed id. Not null means this feed shares other feed.
    /// </summary>
    public string ParentFeedId { get; set; }

    /// <summary>
    /// Selected user ids if discovery option is selected users.
    /// </summary>
    public List<string> DiscoveryOptionSelectedUserIds { get; set; } = [];

    /// <summary>
    /// Created at time of feed.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modified at time of feed.
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}
