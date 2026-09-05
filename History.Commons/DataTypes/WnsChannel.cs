using MongoDB.Bson.Serialization.Attributes;
using System;

namespace History.Commons.DataTypes;

public class WnsChannel
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }
    public string ChannelUri { get; set; }
    public DateTime CreatedAt { get; set; }
}