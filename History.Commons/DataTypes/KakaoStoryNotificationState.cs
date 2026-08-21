using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

/// <summary>
/// Per-user deduplication state for Kakao Story notifications. The server keeps
/// the ids of notifications it has already seen so a poll window overflow (more
/// than 30 new notifications) cannot re-send an already-delivered notification.
/// The id array is trimmed to the newest 100 entries.
/// </summary>
public class KakaoStoryNotificationState
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }

    public List<string> KnownNotificationIds { get; set; }

    public DateTime UpdatedAt { get; set; }
}
