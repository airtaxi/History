using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class CommentLike
{
    [BsonId]
    public string Id { get; set; }
    public string CommentId { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
