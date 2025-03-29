using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class FeedReaction
{
    [BsonId]
    public string Id { get; set; }
    public string FeedId { get; set; }
    public string UserId { get; set; }
    public FeedReactionType ReactionType { get; set; }
    public DateTime CreatedAt { get; set; }
}
