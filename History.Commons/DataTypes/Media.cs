using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Media
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the file name of the media object. This is typically the generated guid string that is used to identify the
    /// media file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Represents the unique identifier for a user who uploaded the media.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents an identifier associated with media.
    /// For example, it can be the post ID for a post media or comment ID for a comment media.
    /// </summary>
    public string AssociatedId { get; set; }

    /// <summary>
    /// Represents the size of the media in bytes.
    /// </summary>
    public long MediaSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME type associated with the content.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Represents the type of media bucket. It can be one of the following:
    /// ProfileMedia
    /// BackgroundMedia
    /// PostMedia
    /// CommentMedia
    /// ...
    /// </summary>
    public MediaBucket BucketType { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created. 
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
