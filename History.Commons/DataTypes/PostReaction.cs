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

    public string PostId { get; set; }
    public string UserId { get; set; }

    /// <summary>
    /// Represents the type of reaction associated with a post. It can indicate various user responses such as like,
    /// love, or dislike.
    /// </summary>
    public PostReactionType ReactionType { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
