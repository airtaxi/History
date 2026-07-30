using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class InviteCode
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// The invite code string (8-char uppercase alphanumeric, ambiguous chars excluded).
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// The user who owns this invite code (the user it was assigned to).
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Whether this invite code is still usable. Set to false on use or withdrawal.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The user who used this invite code to register. Null until used.
    /// </summary>
    public string UsedByUserId { get; set; }

    /// <summary>
    /// When this invite code was used. Null until used.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}