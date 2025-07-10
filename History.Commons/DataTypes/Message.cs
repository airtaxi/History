using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Message
{
    [BsonId]
    public string Id { get; set; }

    public string SenderId { get; set; }
    public string ReceiverId { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}