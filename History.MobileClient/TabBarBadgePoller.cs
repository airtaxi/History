using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Message;
using History.Commons.Api.User;
using History.MobileClient.KakaoStory;

namespace History.MobileClient;

/// <summary>
/// Polls the badge source lists every 10 seconds while the app is in the
/// foreground and records the unread counts for the tab bar badges. The History
/// notification list carries the notification badge count, the received message
/// list the unread mail count, and the pending friend request list the friends
/// tab count; the Kakao Story notification, mail, and invitation lists are polled
/// alongside them for the Kakao Story badge counts. The counts are also refreshed
/// by the list pages themselves; this poller only keeps the badges up to date
/// while the user is elsewhere in the app. The Kakao Story poll section is
/// independent of the Kakao Story notification setting: the badge counts stay
/// fresh even when local notifications are disabled. Each Kakao Story badge
/// category can be excluded from the badge sum via the
/// KakaoStoryNotificationBadgeEnabled/KakaoStoryMailBadgeEnabled/
/// KakaoStoryFriendRequestBadgeEnabled settings; disabled categories are not
/// polled at all. History 401 responses are handled by the ApiHandler token
/// refresh; a failed cycle is skipped silently.
/// </summary>
public static class TabBarBadgePoller
{
    private const bool IsPollLoggingEnabled = true;
    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);
    private static CancellationTokenSource s_foregroundPollingCts;
    private static Task s_foregroundPollingTask;
    private static bool s_isPaused;

    private static void LogPoll(string message)
    {
        if (!IsPollLoggingEnabled) return;
#if ANDROID
        Android.Util.Log.Debug("History", $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
#elif IOS
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
#endif
    }

    /// <summary>
    /// Starts the foreground polling loop (1 request/10 seconds against the
    /// badge source lists while the app is visible). No-op when already running
    /// or paused (see <see cref="Pause"/>).
    /// </summary>
    public static void StartForegroundPolling()
    {
        if (s_isPaused) return;
        if (s_foregroundPollingTask != null) return;

        s_foregroundPollingCts = new CancellationTokenSource();
        LogPoll("Tab bar badge polling started.");
        // Run the loop on a threadpool thread so the poll work (JSON parsing,
        // count computation) never touches the UI thread. The badge view update
        // and messenger sends are already marshalled to the main thread.
        s_foregroundPollingTask = Task.Run(() => RunForegroundPollingLoopAsync(s_foregroundPollingCts.Token));
    }

    /// <summary>
    /// Stops the foreground polling loop. A cycle already in flight finishes;
    /// overlapping cycles are prevented by the poll semaphore.
    /// </summary>
    public static void StopForegroundPolling()
    {
        if (s_foregroundPollingTask == null) return;

        LogPoll("Tab bar badge polling stopped.");
        s_foregroundPollingCts.Cancel();
        s_foregroundPollingTask = null;
        s_foregroundPollingCts = null;
    }

    /// <summary>
    /// Pauses the foreground polling loop (used on logout). The loop stays
    /// paused across window resume cycles until <see cref="TryStart"/> is called.
    /// </summary>
    public static void Pause()
    {
        s_isPaused = true;
        StopForegroundPolling();
    }

    /// <summary>
    /// Resumes the foreground polling loop after a login. Polls once immediately
    /// so the badges reflect the current state right away, then continues on the
    /// 10-second cadence.
    /// </summary>
    public static void TryStart()
    {
        s_isPaused = false;
        _ = PollOnceAsync();
        StartForegroundPolling();
    }

    private static async Task RunForegroundPollingLoopAsync(CancellationToken cancellationToken)
    {
        // A per-loop timer keeps a restarting loop from sharing a timer with a
        // still-finishing previous loop (PeriodicTimer only allows one waiter).
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                // Belt-and-suspenders guard: even if the window lifecycle events
                // fail to stop this loop, it must never poll while the app is not
                // visible. Background coverage is owned by the background pollers.
                if (!App.IsForeground) continue;

                LogPoll("Tab bar badge poll cycle.");
                try { await PollOnceAsync(); }
                catch (Exception exception) { LogPoll($"Tab bar badge poll cycle failed: {exception.Message}"); }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Single poll cycle. Reentrancy is guarded so concurrent cycles never overlap.
    /// </summary>
    public static async Task PollOnceAsync()
    {
        if (s_pollSemaphore.CurrentCount == 0) return;

        try
        {
            await s_pollSemaphore.WaitAsync();

            // Not logged in (or logged out): nothing to poll.
            if (Shared.ApiHandler == ApiHandler.Public) return;

            var notifications = await Shared.ApiHandler.ExecuteRequestAsync(new GetNotifications());
            Shared.HistoryUnreadNotificationCount = notifications?.Count(x => x.IsUnread) ?? 0;

            // The received message list carries the unread mail count; the badge
            // shows it together with the notification count.
            var messages = await Shared.ApiHandler.ExecuteRequestAsync(new GetReceivedMessages());
            Shared.HistoryUnreadMailCount = messages?.Count(x => x.ReadAt == null) ?? 0;

            // The pending request list carries the received friend request count;
            // the friends tab badge shows it together with the Kakao Story count.
            var pendingRequests = await Shared.ApiHandler.ExecuteRequestAsync(new GetPendingRequests());
            Shared.HistoryPendingFriendRequestCount = pendingRequests?.Count ?? 0;

            await PollKakaoStoryBadgesAsync();
        }
        finally { s_pollSemaphore.Release(); }
    }

    private static async Task PollKakaoStoryBadgesAsync()
    {
        // Disabled badge categories are not polled at all; when every category
        // is disabled, skip the Kakao Story session work entirely.
        var isNotificationBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryNotificationBadgeEnabled") ?? true;
        var isMailBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryMailBadgeEnabled") ?? true;
        var isFriendRequestBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryFriendRequestBadgeEnabled") ?? true;
        if (!isNotificationBadgeEnabled && !isMailBadgeEnabled && !isFriendRequestBadgeEnabled) return;

        // A missing Kakao Story session is not an error; the badge stays at its
        // current value (0 on a fresh install).
        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() == null) return;

        // Keep the Kakao Story requests out of the interactive retry budget and
        // suppress the login modal on a 401, mirroring the notification poller.
        var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
        var previousMaxRetryCount = KakaoStoryApiHandler.MaxRetryCount;
        KakaoStoryApiHandler.IsBackgroundMode = true;
        KakaoStoryApiHandler.MaxRetryCount = 2;
        try
        {
            if (isNotificationBadgeEnabled)
            {
                var notifications = await KakaoStoryApiHandler.GetNotifications();
                Shared.KakaoStoryUnreadNotificationCount = notifications?.Count(x => x.is_new) ?? 0;
            }

            if (isMailBadgeEnabled)
            {
                var mails = await KakaoStoryApiHandler.GetMails();
                Shared.KakaoStoryUnreadMailCount = mails?.Count(x => x.type == "receive" && x.read_at == null) ?? 0;
            }

            if (isFriendRequestBadgeEnabled)
            {
                var invitations = await KakaoStoryApiHandler.GetInvitations();
                Shared.KakaoStoryPendingFriendRequestCount = invitations?.Count(x => x.type == "received") ?? 0;
            }
        }
        finally
        {
            KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode;
            KakaoStoryApiHandler.MaxRetryCount = previousMaxRetryCount;
        }
    }
}
