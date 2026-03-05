using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("hashtag")]
public class HashtagContent : BaseContent
{
    public string Tag { get; set; }
}
