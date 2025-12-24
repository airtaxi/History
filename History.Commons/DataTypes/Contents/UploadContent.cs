using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("upload")]
public class UploadContent : BaseContent
{
    public string FileName { get; set; }
    public string Description { get; set; }
    public bool IsSpoiler { get; set; }
}
