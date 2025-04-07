using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("text")]
public class TextContent : BaseContent
{
    public string Text { get; set; }
}
