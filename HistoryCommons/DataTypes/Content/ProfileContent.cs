using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Content;

[BsonDiscriminator("profile")]
public class ProfileContent : BaseContent
{
    public string UserId { get; set; }
}
