using System.Net;
using History.Commons;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.KakaoStory;

/// <summary>
/// Polls the Kakao Story notification counter and raises local notifications for
/// new notifications. Shared by the foreground 1-second poller (started while the
/// window is resumed) and the Android background JobService (15-minute cadence).
/// 401 responses never open the login modal from here; the poll cycle just ends
/// silently and the saved cookies are revalidated on the next cycle.
/// </summary>
public static class KakaoStoryNotificationPoller
{
    private const string LastNotificationCountKey = "KakaoStoryLastNotificationCount";
    private const string KnownNotificationIdsKey = "KakaoStoryKnownNotificationIds";
    private const string IsEnabledKey = "KakaoStoryNotificationEnabled";
    private const int MaxKnownNotificationIds = 200;

    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);
    private static readonly PeriodicTimer s_timer = new(TimeSpan.FromSeconds(1));
    private static CancellationTokenSource s_foregroundPollingCts;
    private static Task s_foregroundPollingTask;
    private static int s_lastNotificationCount = -1;

    /// <summary>
    /// Starts the foreground polling loop (1 request/second against the cheap
    /// new_count endpoint). No-op when the loop is already running.
    /// </summary>
    public static void StartForegroundPolling()
    {
        if (s_foregroundPollingTask != null) return;

        s_foregroundPollingCts = new CancellationTokenSource();
        s_foregroundPollingTask = RunForegroundPollingLoopAsync(s_foregroundPollingCts.Token);
    }

    /// <summary>
    /// Stops the foreground polling loop. A cycle already in flight finishes;
    /// overlapping cycles are prevented by the poll semaphore.
    /// </summary>
    public static void StopForegroundPolling()
    {
        if (s_foregroundPollingTask == null) return;

        s_foregroundPollingCts.Cancel();
        s_foregroundPollingTask = null;
        s_foregroundPollingCts = null;
    }

    private static async Task RunForegroundPollingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await s_timer.WaitForNextTickAsync(cancellationToken))
                await PollOnceAsync();
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Single poll cycle shared by the foreground loop and the background job.
    /// Reentrancy is guarded so concurrent cycles never overlap.
    /// </summary>
    public static async Task PollOnceAsync()
    {
        if (s_pollSemaphore.CurrentCount == 0) return;

        try
        {
            await s_pollSemaphore.WaitAsync();
            await PollCoreAsync();
        }
        finally { s_pollSemaphore.Release(); }
    }

    private static async Task PollCoreAsync()
    {
        // The user can disable Kakao Story notifications from the settings page.
        if (Configuration.GetValue<bool?>(IsEnabledKey) == false) return;

        var cookies = Configuration.GetValue<List<Cookie>>("KakaoStoryCookies");
        if (cookies == null || cookies.Count == 0) return;

        var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
        KakaoStoryApiHandler.IsBackgroundMode = true;
        try
        {
            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies) cookieContainer.Add(cookie);
            KakaoStoryApiHandler.Init(cookieContainer, cookies, null);

            var status = await KakaoStoryApiHandler.GetNotificationStatus();
            if (status == null) return;

            if (s_lastNotificationCount < 0) s_lastNotificationCount = Preferences.Get(LastNotificationCountKey, -1);
            if (status.NotificationCount > s_lastNotificationCount)
                await FetchAndPostNotificationsAsync();

            if (status.NotificationCount != s_lastNotificationCount)
            {
                s_lastNotificationCount = status.NotificationCount;
                Preferences.Set(LastNotificationCountKey, s_lastNotificationCount);
            }
        }
        finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
    }

    private static async Task FetchAndPostNotificationsAsync()
    {
        var notifications = await KakaoStoryApiHandler.GetNotifications();
        if (notifications == null || notifications.Count == 0) return;

        var knownIds = new HashSet<string>(GetKnownNotificationIds());
        var newNotifications = notifications.Where(notification => notification.id != null && knownIds.Add(notification.id)).ToList();
        if (newNotifications.Count == 0) return;

        foreach (var notification in newNotifications) PostNotification(notification);

        SaveKnownNotificationIds([.. knownIds]);
    }

    private static void PostNotification(Notification notification)
    {
#if ANDROID
        KakaoStoryNotificationPoster.Post(notification);
#endif
    }

    private static List<string> GetKnownNotificationIds()
    {
        var raw = Preferences.Get(KnownNotificationIdsKey, string.Empty);
        return string.IsNullOrEmpty(raw) ? [] : [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static void SaveKnownNotificationIds(List<string> ids)
    {
        if (ids.Count > MaxKnownNotificationIds) ids = ids[..MaxKnownNotificationIds];
        Preferences.Set(KnownNotificationIdsKey, string.Join(',', ids));
    }
}
