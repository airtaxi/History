using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

/// <summary>
/// Represents a reaction to a post.
/// </summary>
[BsonIgnoreExtraElements]
public class PostReaction
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the unique identifier for a post that the reaction is associated with.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// Represents the unique identifier for a user who reacted to the post.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents the type of reaction associated with a post.
    /// </summary>
    public PostReactionType Type { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
