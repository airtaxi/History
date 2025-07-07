using History.Commons.Enums;

namespace History.Commons.DataTypes.ResponseDtos;

public class UserResponseDto()
{
    public string UserId { get; set; }
    public string Handle { get; set; }

    public Rank Rank { get; set; }
    public SocialService SocialService { get; set; }

    public DiscoveryOption FriendListDiscoveryOption { get; set; }
    public DiscoveryOption LastUsedPostDiscoveryOption { get; set; }

    public string Nickname { get; set; }
    public string Birthday { get; set; }
    public string Description { get; set; }

    public string ProfileMediaId { get; set; }
    public string ProfileThumbnailMediaId { get; set; }
    public bool UsesAnimatedProfileMedia { get; set; }

    public string BackgroundMediaId { get; set; }
    public string BackgroundThumbnailMediaId { get; set; }
    public bool UsesAnimatedBackgroundMedia { get; set; }

    public bool IsFavorite { get; set; }

    public bool IsFavoriteFriendNewPostPushNotificationEnabled { get; set; }
    public AccessPermission CommentPushNotificationPermission { get; set; }
    public AccessPermission CommentMentionPushNotificationPermission { get; set; }
    public AccessPermission CommentLikePushNotificationPermission { get; set; }
    public AccessPermission SharedPostCommentPushNotificationPermission { get; set; }
    public AccessPermission PostReactionPushNotificationPermission { get; set; }
    public AccessPermission PostMentionPushNotificationPermission { get; set; }

    public Friendship Friendship { get; set; }

    public UserResponseDto(User user) : this()
    {
        UserId = user.Id;
        Handle = user.Handle;

        Rank = user.Rank;
        SocialService = user.SocialService;

        FriendListDiscoveryOption = user.FriendListDiscoveryOption;
        LastUsedPostDiscoveryOption = user.LastUsedPostDiscoveryOption;

        Nickname = user.Nickname;
        Birthday = user.Birthday;
        Description = user.Description;

        ProfileMediaId = user.ProfileMediaId;
        ProfileThumbnailMediaId = user.ProfileThumbnailMediaId;
        UsesAnimatedProfileMedia = user.UsesAnimatedProfileMedia;

        BackgroundMediaId = user.BackgroundThumbnailMediaId;
        BackgroundThumbnailMediaId = user.BackgroundThumbnailMediaId;
        UsesAnimatedBackgroundMedia = false;

        // Push notification settings
        IsFavoriteFriendNewPostPushNotificationEnabled = user.IsFavoriteFriendNewPostPushNotificationEnabled;
        CommentPushNotificationPermission = user.CommentPushNotificationPermission;
        CommentMentionPushNotificationPermission = user.CommentMentionPushNotificationPermission;
        CommentLikePushNotificationPermission = user.CommentLikePushNotificationPermission;
        SharedPostCommentPushNotificationPermission = user.SharedPostCommentPushNotificationPermission;
        PostReactionPushNotificationPermission = user.PostReactionPushNotificationPermission;
        PostMentionPushNotificationPermission = user.PostMentionPushNotificationPermission;
    }
}
