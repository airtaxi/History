using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

/// <summary>
/// Tracks per-user read status for notifications.
/// Id is a composite key: "{NotificationId}_{UserId}".
/// </summary>
public class NotificationRead
{
    [BsonId]
    public string Id { get; set; }

    public string NotificationId { get; set; }
    public string UserId { get; set; }
    public DateTime ReadAt { get; set; }
}
