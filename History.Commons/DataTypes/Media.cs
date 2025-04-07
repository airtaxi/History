using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Media
{
    [BsonId]
    public string Id { get; set; }

    public string FileName { get; set; }
    public string UserId { get; set; }
    public long MediaSize { get; set; }
    public MediaBucket BucketType { get; set; }
}
