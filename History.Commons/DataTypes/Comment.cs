using History.Commons.DataTypes.Content;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Comment
{
    [BsonId]
    public string Id { get; set; }
    
    /// <summary>
    /// Represents the unique identifier for a post.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// Represents the user ID of the author.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// A list that holds instances of BaseContent.
    /// </summary>
    public List<BaseContent> Contents { get; set; } = [];

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Represents the date and time when the entity was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
