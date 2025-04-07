using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class IgnoredPost
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the unique identifier of the user who liked the comment.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents the unique identifier for a comment that the user liked.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
