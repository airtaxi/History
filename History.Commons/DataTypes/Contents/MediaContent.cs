using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("media")]
public class MediaContent : BaseContent
{
    public string MediaId { get; set; }
    public string MimeType { get; set; }

    [MaxLength(CommonsConstants.MaxMediaDescriptionLength)]
    public string Description { get; set; }
}
