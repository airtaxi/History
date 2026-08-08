using System.Net;
using History.Commons;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.KakaoStory;

/// <summary>
/// Polls the Kakao Story notification list and raises local notifications for
/// new notifications. The newest notification id is persisted as a baseline; any
/// item newer than that baseline (the API list is newest-first and capped at 30)
/// is posted. Shared by the foreground 1-second poller (started while the window
/// is resumed) and the Android background JobService (15-minute cadence).
/// 401 responses never open the login modal from here; the poll cycle just ends
/// silently and the saved cookies are revalidated on the next cycle.
/// </summary>
public static class KakaoStoryNotificationPoller
{
    private const string LatestNotificationIdKey = "KakaoStoryLatestNotificationId";
    private const string IsEnabledKey = "KakaoStoryNotificationEnabled";
    private const string SessionExpiredNotificationEnabledKey = "KakaoStorySessionExpiredNotificationEnabled";
    private const string SessionExpiredNotifiedKey = "KakaoStorySessionExpiredNotified";

    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);
    private static readonly PeriodicTimer s_timer = new(TimeSpan.FromSeconds(1));
    private static CancellationTokenSource s_foregroundPollingCts;
    private static Task s_foregroundPollingTask;

    static KakaoStoryNotificationPoller() => KakaoStoryApiHandler.OnBackgroundReloginRequired += HandleSessionExpired;

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

            var notifications = await KakaoStoryApiHandler.GetNotifications();
            if (notifications == null || notifications.Count == 0) return;

            var latestId = notifications[0].id;
            if (string.IsNullOrEmpty(latestId)) return;

            var storedLatestId = Preferences.Get(LatestNotificationIdKey, string.Empty);
            if (string.IsNullOrEmpty(storedLatestId))
            {
                // First poll ever (no baseline recorded yet): the whole history
                // would be treated as new. Only record the baseline so fresh
                // installs do not blast every past notification at once.
                Preferences.Set(LatestNotificationIdKey, latestId);
                return;
            }

            if (storedLatestId == latestId) return; // Nothing new.

            // The list is newest-first (capped at 30): everything newer than the
            // stored baseline is new. When the baseline fell out of the window
            // (more than 30 new notifications), every fetched item is posted.
            // Notifications already read in the app (is_new == false) are skipped,
            // while the baseline still advances past them.
            var newNotifications = new List<Notification>();
            foreach (var notification in notifications)
            {
                if (notification.id == storedLatestId) break;
                if (notification.is_new) newNotifications.Add(notification);
            }

            foreach (var notification in newNotifications) PostNotification(notification);

            Preferences.Set(LatestNotificationIdKey, latestId);
        }
        finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
    }

    private static void PostNotification(Notification notification)
    {
#if ANDROID
        KakaoStoryNotificationPoster.Post(notification);
#endif
    }

    /// <summary>
    /// Posts the "Kakao Story login expired" notification at most once per expired
    /// session. The flag is cleared again once a login succeeds (see
    /// ResetSessionExpiredNotification). The user can disable it from the settings page.
    /// </summary>
    private static void HandleSessionExpired()
    {
        if (Configuration.GetValue<bool?>(SessionExpiredNotificationEnabledKey) == false) return;
        if (Preferences.Get(SessionExpiredNotifiedKey, false)) return; // Already notified for this expired session.

        Preferences.Set(SessionExpiredNotifiedKey, true);
        PostSessionExpiredNotification();
    }

    /// <summary>
    /// Called after a successful Kakao Story login so a future 401 notifies again.
    /// </summary>
    public static void ResetSessionExpiredNotification() => Preferences.Set(SessionExpiredNotifiedKey, false);

    private static void PostSessionExpiredNotification()
    {
#if ANDROID
        KakaoStoryNotificationPoster.PostSessionExpired();
#endif
    }
}
