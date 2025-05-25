using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

public class Notification
{
    [BsonId]
    public string Id { get; set; }

    public IEnumerable<string> Recipients { get; set; }

    public NotificationType Type { get; set; }
    public string AssociatedId { get; set; }

    public string UserId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string ImageUrl { get; set; }

    public bool PushNotificationDisabled { get; set; }

    public Dictionary<string, string> Data { get; set; }

    public DateTime CreatedAt { get; set; }
}
