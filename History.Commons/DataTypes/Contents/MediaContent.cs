using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("media")]
public class MediaContent : BaseContent
{
    public string MediaId { get; set; }
    public string MimeType { get; set; }

    public bool IsVideo => MimeType.StartsWith("video/");

    [MaxLength(CommonsConstants.MaxMediaDescriptionLength)]
    public string Description { get; set; }
}
