using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class FriendRequest
{
    [BsonId]
    public string Id { get; set; }

    public string SenderId { get; set; }
    public string ReceiverId { get; set; }

    public DateTime CreatedAt { get; set; }
}
