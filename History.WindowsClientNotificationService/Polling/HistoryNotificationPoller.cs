using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Ipc;
using History.WindowsClientNotificationService.Core;
using History.WindowsClientNotificationService.Notifications;
using History.WindowsClientNotificationService.State;

namespace History.WindowsClientNotificationService.Polling;

// Polls the History notification list. The first page is enough because new notifications are
// newest-first, and the ids already delivered are persisted so a restart never re-shows them.
public sealed class HistoryNotificationPoller(NotificationStateStore stateStore, ToastPublisher toastPublisher, ClientPostDisplayBridge clientPostDisplayBridge, FileLogger logger)
{
    private const int NotificationPageSize = 30;
    private static readonly TimeSpan MaxNotificationAge = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan TokenBridgeTimeout = TimeSpan.FromSeconds(30);

    private bool _isSignedOut;

    public async Task PollOnceAsync()
    {
        Configuration.ReloadFromDisk();

        var accessToken = Configuration.GetValue<string>("AccessToken");
        var refreshToken = Configuration.GetValue<string>("RefreshToken");
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            HandleSignedOut();
            return;
        }

        _isSignedOut = false;

        // The running client owns the shared token file, so when it is up it performs the refresh
        // and this service just reads the result.
        var bridgedTokens = await TryGetTokensFromRunningClientAsync();
        if (bridgedTokens != null && !string.IsNullOrEmpty(bridgedTokens.RefreshToken))
        {
            accessToken = bridgedTokens.AccessToken;
            refreshToken = bridgedTokens.RefreshToken;
        }

        var apiHandler = new ApiHandler(accessToken, refreshToken);
        CommonShared.ApiHandler = apiHandler;

        List<NotificationResponseDto> notifications;
        try { notifications = await apiHandler.ExecuteRequestAsync(new GetNotifications(null, NotificationPageSize)); }
        catch (Exception exception)
        {
            logger.Log($"History notification fetch failed: {exception.Message}");
            return;
        }

        if (!stateStore.IsHistoryInitialized)
        {
            stateStore.RecordHistoryNotifications(notifications.Select(notification => notification.Id));
            stateStore.MarkHistoryInitialized();
            logger.Log("History notification baseline recorded.");
            return;
        }

        var newNotifications = notifications
            .Where(notification => notification.Id != null && !stateStore.IsHistoryNotificationKnown(notification.Id))
            .Where(notification => NotificationTimestamp.ToUtc(notification.CreatedAt) > DateTime.UtcNow - MaxNotificationAge)
            .OrderBy(notification => notification.CreatedAt)
            .ToList();

        foreach (var notification in newNotifications)
        {
            if (ShouldDeferToClient(notification)) continue;
            toastPublisher.Show(notification.Title, notification.Body, notification.ImageUrl, notification.Data);
        }

        stateStore.RecordHistoryNotifications(notifications.Select(notification => notification.Id));
    }

    // A notification that targets the post the client currently shows is handled by the client
    // through a reload instead of a toast when the client's main window is already active.
    private bool ShouldDeferToClient(NotificationResponseDto notification) => notification.Data != null && notification.Data.TryGetValue("PostId", out var postId) && clientPostDisplayBridge.ShouldSuppressToast(NotificationPostPlatform.History, postId);

    // Asks the running client to refresh and return the tokens. Failures are silent because the
    // service's own refresh path (guarded by the cross-process lock) is the fallback.
    private static async Task<TokenBridgeResponse> TryGetTokensFromRunningClientAsync()
    {
        if (!AppTokenBridgeClient.Instance.IsClientRunning()) return null;

        var response = await AppTokenBridgeClient.Instance.TrySendAsync(new TokenBridgeRequest { Kind = TokenBridgeRequestKind.EnsureHistoryTokens }, TokenBridgeTimeout);
        return response?.IsSuccess == true ? response : null;
    }

    private void HandleSignedOut()
    {
        if (_isSignedOut) return;

        _isSignedOut = true;
        logger.Log("Signed out; History notification polling paused.");
        toastPublisher.ClearHistory();
    }
}
