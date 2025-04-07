using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("media")]
public class MediaContent : BaseContent
{
    public string MediaId { get; set; }
    public string Description { get; set; }
}
