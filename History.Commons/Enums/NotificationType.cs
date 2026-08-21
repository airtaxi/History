using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType
{
    Comment,
    CommentMention,
    CommentLike,
    Share,
    Repost,
    PostReaction,
    PostMention,
    FriendRequest,
    FavoriteFriendNewPost,
    Birthday,
    Restriction,
    Report,
    Message,
    InviteCodeRequest,
    InviteCodeRequestResult,
    KakaoStory
}
