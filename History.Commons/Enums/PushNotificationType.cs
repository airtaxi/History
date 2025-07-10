using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PushNotificationType>))]
public enum PushNotificationType
{
    Comment,
    CommentMention,
    CommentLike,
    SharedPostComment,
    PostReaction,
    PostMention,
    FavoriteFriendNewPost,
    Message
}

public static class PushNotificationTypeExtensions
{
    public static string ToDisplayString(this PushNotificationType type)
    {
        return type switch
        {
            PushNotificationType.Comment => "댓글",
            PushNotificationType.CommentMention => "댓글 언급",
            PushNotificationType.CommentLike => "댓글 좋아요",
            PushNotificationType.SharedPostComment => "공유된 게시글 댓글",
            PushNotificationType.PostReaction => "게시물 반응",
            PushNotificationType.PostMention => "게시물 언급",
            PushNotificationType.FavoriteFriendNewPost => "관심 친구의 새 게시글",
            PushNotificationType.Message => "쪽지",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}