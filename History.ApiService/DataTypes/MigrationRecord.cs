using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace History.ApiService.DataTypes;

public class MigrationRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }
}
