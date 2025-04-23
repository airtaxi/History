using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    public string Id { get; set; }

    public string Handle { get; set; }

    /// <summary>
    /// Rank of user.
    /// </summary>
    public Rank Rank { get; set; }

    /// <summary>
    /// Social service that user used to sign up.
    /// </summary>
    public SocialService SocialService { get; set; }

    /// <summary>
    /// User's nickname.
    /// </summary>
    public string Nickname { get; set; }

    /// <summary>
    /// User's birthday. Null if user did not set or don't want to.
    /// </summary>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// User profile description set by user.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Profile media id.
    /// </summary>
    public string ProfileMediaId { get; set; }

    /// <summary>
    /// Determines whether the user has set a video as their profile media.
    /// </summary>
    public bool UsesAnimatedProfileMedia { get; set; }

    /// <summary>
    /// Background media id.
    /// </summary>
    public string BackgroundMediaId { get; set; }

    /// <summary>
    /// Determines whether the user has set a video as their background.
    /// </summary>
    public bool UsesAnimatedBackgroundMedia { get; set; }

    /// <summary>
    /// Discovery option for friend list.
    /// </summary>
    public DiscoveryOption FriendListDiscoveryOption { get; set; } = DiscoveryOption.Everyone;

    /// <summary>
    /// Represents the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
