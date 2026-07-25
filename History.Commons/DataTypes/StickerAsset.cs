using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class StickerAsset
{
    [BsonId]
    public string Id { get; set; }

    public string MediaId { get; set; }
    public string StickerId { get; set; }
    public bool IsAnimated { get; set; }
}
