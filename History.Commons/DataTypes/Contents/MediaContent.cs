using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("media")]
public class MediaContent : BaseContent
{
    public string MediaId { get; set; }
    public string MimeType { get; set; }

    // Optional
    public string ThumbnailMediaId { get; set; }

    public bool IsVideo => MimeType.StartsWith("video/");

    public bool IsSpoiler { get; set; }

    [MaxLength(CommonConstants.MaxMediaDescriptionLength)]
    public string Description { get; set; }
}
