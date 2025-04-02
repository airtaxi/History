using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class PostReaction
{
    [BsonId]
    public string Id { get; set; }
    public string PostId { get; set; }
    public string UserId { get; set; }
    public PostReactionType ReactionType { get; set; }
    public DateTime CreatedAt { get; set; }
}
