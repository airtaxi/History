using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Sticker
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the name of the sticker.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Represents the category of the sticker.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Represents the unique identifier for the author of the sticker.
    /// </summary>
    public string AuthorId { get; set; }

    /// <summary>
    /// Represents the description for the sticker.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Represents the media ID for an icon associated with an sticker.
    /// </summary>
    public string IconMediaId { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Represents the date and time when the sticker was last modified. It is nullable, allowing for the absence of a
    /// modification date.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Indicates whether the sticker is private. (Author only)
    /// </summary>
    public bool IsPrivate { get; set; }
}
