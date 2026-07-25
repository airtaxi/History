using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("sticker")]
public class StickerContent : BaseContent
{
    public string StickerId { get; set; }
    public string StickerContentId { get; set; }
    public string StickerMediaId { get; set; } // Read only, must be set when creating dto
    public bool IsAnimated { get; set; } // Read only, must be set when creating dto
}
