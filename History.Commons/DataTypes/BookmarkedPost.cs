using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class BookmarkedPost
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the unique identifier of the user who bookmarked the post.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents the unique identifier for a post that the user bookmarked.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// Represents the date and time when the bookmark was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
