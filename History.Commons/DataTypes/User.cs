using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    public string Id { get; set; }

    /// <summary>
    /// User's email address. (from social service)
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Customized user ID.
    /// Default: 8-digit UUID
    /// </summary>
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
    public string Birthday { get; set; }

    /// <summary>
    /// User profile description set by user.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Profile media id.
    /// </summary>
    public string ProfileMediaId { get; set; }

    /// <summary>
    /// Profile thumbnail media id.
    /// </summary>
    public string ProfileThumbnailMediaId { get; set; }

    /// <summary>
    /// Determines whether the user has set a video as their profile media.
    /// </summary>
    public bool UsesAnimatedProfileMedia { get; set; }

    /// <summary>
    /// Background media id.
    /// </summary>
    public string BackgroundMediaId { get; set; }

    /// <summary>
    /// Background thumbnail media id.
    /// </summary>
    public string BackgroundThumbnailMediaId { get; set; }

    /// <summary>
    /// Determines whether the user has set a video as their background.
    /// </summary>
    public bool UsesAnimatedBackgroundMedia { get; set; }

    /// <summary>
    /// Discovery option for friend list.
    /// </summary>
    public DiscoveryOption FriendListDiscoveryOption { get; set; } = DiscoveryOption.Everyone;

    /// <summary>
    /// Represents the ID of the user's pinned post.
    /// </summary>
    public string PinnedPostId { get; set; }

    /// <summary>
    /// Discovery option for post.
    /// </summary>
    public DiscoveryOption LastUsedPostDiscoveryOption { get; set; } = DiscoveryOption.FriendsOfFriends;

    /// <summary>
    /// If true, the user can be searched by their handle or nickname.
    /// </summary>
    public bool AllowSearch { get; set; } = true;

    /// <summary>
    /// Message receiving permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission MessageReceivingPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Comment push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission CommentPushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Comment mention push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission CommentMentionPushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Comment like push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission CommentLikePushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Shared post comment push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission SharedPostCommentPushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Post reaction push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission PostReactionPushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Post mention push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission PostMentionPushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Message push notification permission.
    /// </summary>
    [BsonDefaultValue(AccessPermission.Everyone)]
    public AccessPermission MessagePushNotificationPermission { get; set; } = AccessPermission.Everyone;

    /// <summary>
    /// Gets or sets a value indicating whether push notifications for new posts by favorite friends are enabled.
    /// </summary>
    [BsonDefaultValue(true)]
    public bool IsFavoriteFriendNewPostPushNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Represents the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
