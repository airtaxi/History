using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Content;

[BsonDiscriminator("sticker")]
public class StickerContent : BaseContent
{
    public string StickerId { get; set; }
}
