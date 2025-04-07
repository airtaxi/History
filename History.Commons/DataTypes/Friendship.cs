using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class Friendship
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Represents the unique identifier of a source user in the friendship.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Represents the unique identifier of a target user in the friendship.
    /// </summary>
    public string FriendId { get; set; }

    /// <summary>
    /// Represents the current status of a friendship. It can indicate various states such as requested, accepted, or
    /// blocked.
    /// </summary>
    public FriendshipStatus Status { get; set; }

    /// <summary>
    /// Represents the date and time when the object was created. 
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
