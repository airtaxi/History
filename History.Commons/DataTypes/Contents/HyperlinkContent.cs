using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("hyperlink")]
public class HyperlinkContent : BaseContent
{
    public string Url { get; set; }
}
