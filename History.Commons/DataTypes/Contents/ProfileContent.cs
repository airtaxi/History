using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("profile")]
public class ProfileContent : BaseContent
{
    public string UserId { get; set; }
}
