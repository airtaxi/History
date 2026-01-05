using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class RecentStickerUsage
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Sticker ID
    /// </summary>
    public string StickerId { get; set; }

    /// <summary>
    /// Sticker asset ID
    /// </summary>
    public string StickerAssetId { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime LastUsedAt { get; set; }
}
