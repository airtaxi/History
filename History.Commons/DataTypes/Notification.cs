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
    public IEnumerable<string> PushNotificationRecipients { get; set; }

    public Dictionary<string, string> Data { get; set; }

    public DateTime CreatedAt { get; set; }

    public Notification Clone()
    {
        return new Notification
        {
            Id = Id,
            Recipients = Recipients?.ToList(),
            Type = Type,
            AssociatedId = AssociatedId,
            UserId = UserId,
            Title = Title,
            Body = Body,
            ImageUrl = ImageUrl,
            PushNotificationDisabled = PushNotificationDisabled,
            Data = Data != null ? new Dictionary<string, string>(Data) : null,
            CreatedAt = CreatedAt
        };
    }
}
