using History.Commons.DataTypes.Content;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Comment
{
    [BsonId]
    public string Id { get; set; }
    public string ParentFeedId { get; set; }

    public List<BaseContent> Contents { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
