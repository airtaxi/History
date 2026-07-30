using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class InviteCodeRequest
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// The user who requested invite codes.
    /// </summary>
    public string RequesterId { get; set; }

    /// <summary>
    /// Optional reason for the request.
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// The number of invite codes requested.
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// The status of the request.
    /// </summary>
    public InviteCodeRequestStatus Status { get; set; } = InviteCodeRequestStatus.Pending;

    /// <summary>
    /// The moderator who processed (accepted/rejected) this request.
    /// </summary>
    public string ModeratorId { get; set; }

    /// <summary>
    /// Optional message from the moderator included in the result notification.
    /// </summary>
    public string ModeratorMessage { get; set; }

    /// <summary>
    /// The number of invite codes actually granted (set on accept).
    /// </summary>
    public int GrantedCount { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was processed (accepted/rejected). Null until processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}