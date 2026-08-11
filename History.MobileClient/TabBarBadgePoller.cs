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
/// fresh even when local notifications are disabled. History 401 responses are
/// handled by the ApiHandler token refresh; a failed cycle is skipped silently.
/// </summary>
public static class TabBarBadgePoller
{
    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);
    private static CancellationTokenSource s_foregroundPollingCts;
    private static Task s_foregroundPollingTask;
    private static bool s_isPaused;

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
    /// Resumes the foreground polling loop after a login.
    /// </summary>
    public static void TryStart()
    {
        s_isPaused = false;
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
                try { await PollOnceAsync(); }
                catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Tab bar badge poll cycle failed: {exception.Message}"); }
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
            var notifications = await KakaoStoryApiHandler.GetNotifications();
            Shared.KakaoStoryUnreadNotificationCount = notifications?.Count(x => x.is_new) ?? 0;

            var mails = await KakaoStoryApiHandler.GetMails();
            Shared.KakaoStoryUnreadMailCount = mails?.Count(x => x.type == "receive" && x.read_at == null) ?? 0;

            var invitations = await KakaoStoryApiHandler.GetInvitations();
            Shared.KakaoStoryPendingFriendRequestCount = invitations?.Count(x => x.type == "received") ?? 0;
        }
        finally
        {
            KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode;
            KakaoStoryApiHandler.MaxRetryCount = previousMaxRetryCount;
        }
    }
}
