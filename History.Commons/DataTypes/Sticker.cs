using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Sticker
{
    [BsonId]
    public string Id { get; set; }

    public string Name { get; set; }
    public string Category { get; set; }

    public string AuthorId { get; set; }
    public string Description { get; set; }
    public string IconMediaId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
