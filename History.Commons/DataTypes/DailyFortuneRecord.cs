using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class DailyFortuneRecord
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }

    // KST yyyy-MM-dd
    public string Date { get; set; }
}