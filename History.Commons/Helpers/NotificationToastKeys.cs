using History.Commons.Enums;

namespace History.Commons.Helpers;

// Builds the toast group key that ties a notification's toast to the post it belongs to. The
// Windows client and the background notification service share this key so the service can
// dismiss every toast for a post once the client reports the post as read.
public static class NotificationToastKeys
{
    public static string BuildPostGroup(NotificationPostPlatform platform, string postId) => string.IsNullOrEmpty(postId) ? null : platform switch
    {
        NotificationPostPlatform.History => $"history-post-{postId}",
        NotificationPostPlatform.KakaoStory => $"kakao-post-{postId}",
        _ => null
    };
}
