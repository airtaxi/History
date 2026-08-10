using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class MutedPost
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the unique identifier of the user who muted the post notifications.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents the unique identifier of the post whose notifications are muted.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
