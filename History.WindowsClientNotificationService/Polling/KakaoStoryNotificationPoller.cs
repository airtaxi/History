using History.Commons;
using History.Commons.Ipc;
using History.Commons.KakaoStory;
using History.WindowsClientNotificationService.Core;
using History.WindowsClientNotificationService.Notifications;
using History.WindowsClientNotificationService.State;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClientNotificationService.Polling;

// Polls the Kakao Story notification list with the same filtering and de-duplication the
// server-side poller applies, so the local notifications match what the push path will deliver
// once the WNS channel is live.
public sealed class KakaoStoryNotificationPoller(NotificationStateStore stateStore, ToastPublisher toastPublisher, FileLogger logger)
{
    private static readonly TimeSpan MaxNotificationAge = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan ServerTokenUploadInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan TokenBridgeTimeout = TimeSpan.FromSeconds(30);

    private DateTime _lastServerTokenUploadUtc = DateTime.MinValue;

    public async Task PollOnceAsync()
    {
        Configuration.ReloadFromDisk();
        if (!IsEnabled()) return;

        // The Kakao Story session belongs to the signed-in account, so a signed-out app keeps
        // Kakao Story notifications off as well.
        if (string.IsNullOrEmpty(Configuration.GetValue<string>("AccessToken"))) return;

        if (!await EnsureKAuthTokenAsync()) return;

        await TryUploadTokenToServerAsync();

        var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
        var previousMaxRetryCount = KakaoStoryApiHandler.MaxRetryCount;
        KakaoStoryApiHandler.IsBackgroundMode = true;
        KakaoStoryApiHandler.MaxRetryCount = 2;

        List<Notification> notifications;
        try { notifications = await KakaoStoryApiHandler.GetNotifications(); }
        catch (Exception exception)
        {
            logger.Log($"Kakao Story notification fetch failed: {exception.Message}");
            return;
        }
        finally
        {
            KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode;
            KakaoStoryApiHandler.MaxRetryCount = previousMaxRetryCount;
        }

        if (notifications == null) return;

        if (!stateStore.IsKakaoStoryInitialized)
        {
            stateStore.RecordKakaoStoryNotifications(notifications.Select(notification => notification.id));
            stateStore.MarkKakaoStoryInitialized();
            logger.Log("Kakao Story notification baseline recorded.");
            return;
        }

        var isFavoriteFriendNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryFavoriteFriendNotificationEnabled") ?? true;
        var isEmotionNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryEmotionNotificationEnabled") ?? true;

        var newNotifications = notifications
            .Where(notification => notification.is_new && notification.id != null && !stateStore.IsKakaoStoryNotificationKnown(notification.id))
            .Where(notification => !IsFavoriteFriendNotification(notification) || isFavoriteFriendNotificationEnabled)
            .Where(notification => !IsEmotionNotification(notification) || isEmotionNotificationEnabled)
            .Where(notification => NotificationTimestamp.ToUtc(notification.created_at) > DateTime.UtcNow - MaxNotificationAge)
            .OrderBy(notification => notification.created_at)
            .ToList();

        foreach (var notification in newNotifications) toastPublisher.Show(notification.message ?? "카카오스토리 알림", notification.content, notification.thumbnail_url, BuildToastData(notification));

        stateStore.RecordKakaoStoryNotifications(notifications.Select(notification => notification.id));
    }

    private static bool IsEnabled()
    {
        if ((Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false) == false) return false;
        return Configuration.GetValue<bool?>("KakaoStoryNotificationEnabled") ?? true;
    }

    // While the client runs it owns the shared settings file, so the token refresh is delegated
    // to it; otherwise the service refreshes the persisted Kakao Story session itself.
    private async Task<bool> EnsureKAuthTokenAsync()
    {
        if (AppTokenBridgeClient.Instance.IsClientRunning())
        {
            var response = await AppTokenBridgeClient.Instance.TrySendAsync(new TokenBridgeRequest { Kind = TokenBridgeRequestKind.EnsureKakaoStoryToken }, TokenBridgeTimeout);
            if (response?.IsSuccess == true && !string.IsNullOrEmpty(response.KakaoStoryIdToken))
            {
                Configuration.ReloadFromDisk();
                return true;
            }
        }

        return await KakaoStoryApiHandler.EnsureKAuthTokenAsync() != null;
    }

    // Keeps the server-side polling session alive the same way the mobile background job does:
    // the session lives in memory on the API server and is lost when it restarts.
    private async Task TryUploadTokenToServerAsync()
    {
        if (DateTime.UtcNow - _lastServerTokenUploadUtc < ServerTokenUploadInterval) return;

        _lastServerTokenUploadUtc = DateTime.UtcNow;
        await CommonKakaoStoryUtils.UploadTokenToServerAsync();
    }

    private static Dictionary<string, string> BuildToastData(Notification notification)
    {
        var data = new Dictionary<string, string>
        {
            { "Type", "KakaoStory" },
            { "NotificationId", notification.id }
        };
        if (notification.scheme != null) data["Scheme"] = notification.scheme;
        return data;
    }

    private static bool IsFavoriteFriendNotification(Notification notification) => notification.decorators is { Count: > 0 } && notification.decorators[0].text?.StartsWith("관심친구") == true;

    private static bool IsEmotionNotification(Notification notification) => notification.emotion != null;
}
