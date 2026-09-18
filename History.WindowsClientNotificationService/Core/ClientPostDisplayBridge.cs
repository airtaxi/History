using History.Commons.Enums;
using History.Commons.Ipc;

namespace History.WindowsClientNotificationService.Core;

// Checks a new post notification against the post the running client currently shows. A match
// asks the client to reload that post, and the toast is suppressed while the client's main window
// is active because the user is already looking at the post.
public sealed class ClientPostDisplayBridge(FileLogger logger) : IDisposable
{
    private readonly ClientPostDisplayChannel _channel = ClientPostDisplayChannel.TryCreate();

    public bool ShouldSuppressToast(NotificationPostPlatform platform, string postId)
    {
        if (_channel == null || string.IsNullOrEmpty(postId)) return false;
        if (!AppTokenBridgeClient.Instance.IsClientRunning()) return false;
        if (!_channel.TryGetDisplayedPost(out var displayedPlatform, out var displayedPostId, out var isForeground)) return false;
        if (displayedPlatform != platform || displayedPostId != postId) return false;

        // The client reloads the open post even while its window is in the background, so the post
        // is current when the user returns; only the toast depends on the window being active.
        _channel.RequestPostRefresh(platform, postId);
        _channel.SignalPostRefresh();
        logger.Log($"Client refresh requested for the open post: {platform} {postId}");

        if (!isForeground) return false;

        logger.Log($"Toast suppressed for the post open in the client: {platform} {postId}");
        return true;
    }

    public void Dispose() => _channel?.Dispose();
}
