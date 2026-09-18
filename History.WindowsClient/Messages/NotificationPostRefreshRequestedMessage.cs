using History.Commons.Enums;

namespace History.WindowsClient.Messages;

// A post that the background notification service saw activity for; the post detail page reloads
// when the identified post is the one it currently shows.
public sealed class NotificationPostRefreshRequestedMessage(NotificationPostPlatform platform, string postId)
{
    public NotificationPostPlatform Platform { get; } = platform;

    public string PostId { get; } = postId;
}
