using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes.Contents;

[BsonDiscriminator("poll")]
public class PollContent : BaseContent
{
    /// <summary>
    /// Unique identifier for the poll.
    /// </summary>
    public string PollId { get; set; }

    /// <summary>
    /// Poll question/title.
    /// </summary>
    public string Question { get; set; }

    /// <summary>
    /// List of poll options.
    /// </summary>
    public List<PollOption> Options { get; set; } = [];

    /// <summary>
    /// Whether users can select multiple options.
    /// </summary>
    public bool AllowMultipleSelection { get; set; }

    /// <summary>
    /// Poll expiration time. Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the poll is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    // Read only fields - filled when generating response DTO
    /// <summary>
    /// Total number of votes.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Current user's selected option indices. Null if not voted.
    /// </summary>
    public List<int> MyVotedOptionIndices { get; set; }
}

public class PollOption
{
    /// <summary>
    /// Option text.
    /// </summary>
    public string Text { get; set; }

    // Read only field - filled when generating response DTO
    /// <summary>
    /// Number of votes for this option.
    /// </summary>
    public int VoteCount { get; set; }
}
