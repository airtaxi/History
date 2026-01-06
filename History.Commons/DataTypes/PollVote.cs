using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

public class PollVote
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// Poll ID (from PollContent.PollId).
    /// </summary>
    public string PollId { get; set; }

    /// <summary>
    /// Post ID containing this poll.
    /// </summary>
    public string PostId { get; set; }

    /// <summary>
    /// User ID who voted.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Selected option indices.
    /// </summary>
    public List<int> SelectedOptionIndices { get; set; } = [];

    /// <summary>
    /// Vote creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
