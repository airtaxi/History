using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Enums;
using History.Commons.Ipc;
using History.WindowsClient.Messages;
using History.WindowsClient.Views;

namespace History.WindowsClient.Services;

// Shares the post the detail page currently shows, and whether the main window is active, with
// the background notification service so it can suppress a toast for a post the user already has
// open. In return it reloads that post when the service reports new activity for it.
public sealed partial class NotificationPostDisplayService : IDisposable
{
    private readonly ClientPostDisplayChannel _channel = ClientPostDisplayChannel.TryCreate();
    private RegisteredWaitHandle _refreshRegistration;

    public void Start()
    {
        if (_channel?.RefreshWaitHandle == null) return;

        // A signal left over from an earlier session belongs to a post surface that no longer exists.
        _channel.ResetPostRefreshSignal();
        _refreshRegistration = ThreadPool.RegisterWaitForSingleObject(_channel.RefreshWaitHandle, OnPostRefreshSignaled, null, Timeout.Infinite, executeOnlyOnce: true);
    }

    public void SetDisplayedPost(NotificationPostPlatform platform, string postId) => _channel?.PublishDisplayedPost(platform, postId);

    public void ClearDisplayedPost() => _channel?.ClearDisplayedPost();

    public void SetMainWindowForeground(bool isForeground) => _channel?.PublishIsForeground(isForeground);

    // Runs on a thread pool thread when the notification service asks for a refresh. The request
    // details come from the shared map and the reload itself moves to the UI thread.
    private void OnPostRefreshSignaled(object state, bool timedOut)
    {
        _refreshRegistration?.Unregister(null);
        _refreshRegistration = null;

        try
        {
            if (_channel == null || !_channel.TryGetRefreshRequest(out var platform, out var postId)) return;

            var dispatcherQueue = MainWindow.Instance?.DispatcherQueue;
            if (dispatcherQueue == null) return;
            if (dispatcherQueue.HasThreadAccess) SendRefreshRequest(platform, postId);
            else dispatcherQueue.TryEnqueue(() => SendRefreshRequest(platform, postId));
        }
        finally
        {
            if (_channel?.RefreshWaitHandle != null)
            {
                _refreshRegistration = ThreadPool.RegisterWaitForSingleObject(_channel.RefreshWaitHandle, OnPostRefreshSignaled, null, Timeout.Infinite, executeOnlyOnce: true);
            }
        }
    }

    private static void SendRefreshRequest(NotificationPostPlatform platform, string postId) => WeakReferenceMessenger.Default.Send(new NotificationPostRefreshRequestedMessage(platform, postId));

    public void Dispose()
    {
        _refreshRegistration?.Unregister(null);
        _refreshRegistration = null;
        _channel?.Dispose();
    }
}
