using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class StickerSubscription
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Subscribed user ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Subscribed sticker ID
    /// </summary>
    public string StickerId { get; set; }

    /// <summary>
    /// Subscription date
    /// </summary>
    public DateTime SubscribedAt { get; set; }
}
