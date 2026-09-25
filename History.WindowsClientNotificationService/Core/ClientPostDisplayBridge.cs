using History.Commons.Enums;
using History.Commons.Ipc;
using History.WindowsClientNotificationService.Notifications;

namespace History.WindowsClientNotificationService.Core;

// Checks a new post notification against the post the running client currently shows. A match
// asks the client to reload that post, and the toast is suppressed while the client's main window
// is active because the user is already looking at the post. The same channel carries the client's
// read signals, which dismiss the toasts for a post the user has read.
public sealed class ClientPostDisplayBridge(FileLogger logger, ToastPublisher toastPublisher) : IDisposable
{
    private readonly ClientPostDisplayChannel _channel = ClientPostDisplayChannel.TryCreate();

    private RegisteredWaitHandle _postReadRegistration;

    // Starts waiting for the client's read signals; called once when the service comes up.
    public void Start()
    {
        if (_channel?.PostReadWaitHandle == null) return;

        // A signal left over from an earlier session belongs to a post surface that no longer exists.
        _channel.ResetPostReadSignal();
        _postReadRegistration = ThreadPool.RegisterWaitForSingleObject(_channel.PostReadWaitHandle, OnPostReadSignaled, null, Timeout.Infinite, executeOnlyOnce: true);
    }

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

    // Runs on a thread pool thread when the client reports a post as read. The pending read comes
    // from the shared map, and the matching toasts are dismissed from the action center.
    private void OnPostReadSignaled(object state, bool timedOut)
    {
        _postReadRegistration?.Unregister(null);
        _postReadRegistration = null;

        try
        {
            if (_channel != null && _channel.TryGetPostRead(out var platform, out var postId))
            {
                toastPublisher.RemovePostToasts(platform, postId);
                logger.Log($"Toasts dismissed for the read post: {platform} {postId}");
            }
        }
        catch (Exception exception) { logger.Log($"Post read handling failed: {exception.Message}"); }
        finally
        {
            if (_channel?.PostReadWaitHandle != null) _postReadRegistration = ThreadPool.RegisterWaitForSingleObject(_channel.PostReadWaitHandle, OnPostReadSignaled, null, Timeout.Infinite, executeOnlyOnce: true);
        }
    }

    public void Dispose()
    {
        _postReadRegistration?.Unregister(null);
        _postReadRegistration = null;
        _channel?.Dispose();
    }
}
