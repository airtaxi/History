using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

public class FavoriteFriend
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }
    public string FriendId { get; set; }

    public DateTime CreatedAt { get; set; }
}
