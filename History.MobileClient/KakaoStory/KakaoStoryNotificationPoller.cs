using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.KakaoStory;

/// <summary>
/// Polls the Kakao Story notification and mail lists and raises local
/// notifications for new items. The newest notification id is persisted as a
/// baseline; any item newer than that baseline (the API list is newest-first and
/// capped at 30) is posted. A bounded set of already-posted ids (50) guards
/// against re-posting when the baseline falls out of the 30-item window. Shared
/// by the foreground 5-second poller (started
/// while the window is resumed) and the Android background JobService
/// (15-minute cadence). 401 responses never open the login modal from here; the
/// poll cycle just ends silently and the saved cookies are revalidated on the
/// next cycle. The tab bar badges are not this poller's concern: they are kept
/// up to date by <see cref="TabBarBadgePoller"/> and the list pages.
/// </summary>
public static class KakaoStoryNotificationPoller
{
    private const string LatestNotificationIdKey = "KakaoStoryLatestNotificationId";
    private const string LatestMailIdKey = "KakaoStoryLatestMailId";
    private const string KnownNotificationIdsKey = "KakaoStoryKnownNotificationIds";
    private const string KnownMailIdsKey = "KakaoStoryKnownMailIds";
    private const int MaxKnownIds = 50;
    private const bool IsPollLoggingEnabled = true;
    private const string IsEnabledKey = "KakaoStoryNotificationEnabled";
    private const string FavoriteFriendNotificationEnabledKey = "KakaoStoryFavoriteFriendNotificationEnabled";
    private const string EmotionNotificationEnabledKey = "KakaoStoryEmotionNotificationEnabled";
    private const string MailNotificationEnabledKey = "KakaoStoryMailNotificationEnabled";
    private const string SessionExpiredNotificationEnabledKey = "KakaoStorySessionExpiredNotificationEnabled";
    private const string SessionExpiredNotifiedKey = "KakaoStorySessionExpiredNotified";

    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);
    private static CancellationTokenSource s_foregroundPollingCts;
    private static Task s_foregroundPollingTask;
    private static bool s_isPaused;

    static KakaoStoryNotificationPoller() => KakaoStoryApiHandler.OnBackgroundReloginRequired += HandleSessionExpired;

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
    /// Starts the foreground polling loop (1 request/5 seconds against the
    /// notification list). No-op when the loop is already running or paused
    /// (see <see cref="Pause"/>).
    /// </summary>
    public static void StartForegroundPolling()
    {
        if (s_isPaused) return;
        if (s_foregroundPollingTask != null) return;

        s_foregroundPollingCts = new CancellationTokenSource();
        LogPoll("Kakao Story foreground polling started.");
        // Run the loop on a threadpool thread so the poll work (JSON parsing,
        // notification posting) never touches the UI thread. The messenger sends
        // and local notification posts are already marshalled to the main thread.
        s_foregroundPollingTask = Task.Run(() => RunForegroundPollingLoopAsync(s_foregroundPollingCts.Token));
    }

    /// <summary>
    /// Stops the foreground polling loop. A cycle already in flight finishes;
    /// overlapping cycles are prevented by the poll semaphore.
    /// </summary>
    public static void StopForegroundPolling()
    {
        if (s_foregroundPollingTask == null) return;

        LogPoll("Kakao Story foreground polling stopped.");
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
    /// Resumes the foreground polling loop after a login, respecting the
    /// Kakao Story notification setting.
    /// </summary>
    public static void TryStart()
    {
        s_isPaused = false;
        if ((Configuration.GetValue<bool?>(IsEnabledKey) ?? true) == false) return;
        StartForegroundPolling();
    }

    private static async Task RunForegroundPollingLoopAsync(CancellationToken cancellationToken)
    {
        // A per-loop timer keeps a restarting loop from sharing a timer with a
        // still-finishing previous loop (PeriodicTimer only allows one waiter).
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                // Belt-and-suspenders guard: even if the window lifecycle events
                // fail to stop this loop, it must never poll while the app is not
                // visible. Background coverage is owned by the background pollers.
                if (!App.IsForeground) continue;

                LogPoll("Kakao Story poll cycle.");
                try { await PollOnceAsync(); }
                catch (Exception exception) { LogPoll($"Kakao Story poll cycle failed: {exception.Message}"); }
            }
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

            // Bound retries so a poll cycle fits the platform background budget
            // (e.g. the iOS BGAppRefreshTask execution window) instead of the
            // interactive default of 15 retries.
            var previousMaxRetryCount = KakaoStoryApiHandler.MaxRetryCount;
            KakaoStoryApiHandler.MaxRetryCount = 2;
            try { await PollCoreAsync(); }
            finally { KakaoStoryApiHandler.MaxRetryCount = previousMaxRetryCount; }
        }
        finally { s_pollSemaphore.Release(); }
    }

    private static async Task PollCoreAsync()
    {
        // The user can disable Kakao Story notifications from the settings page.
        if ((Configuration.GetValue<bool?>(IsEnabledKey) ?? true) == false) return;

        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() == null) return;

        var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
        KakaoStoryApiHandler.IsBackgroundMode = true;
        try
        {
            await FetchAndPostNotificationsAsync();
            await FetchAndPostMailsAsync();
        }
        finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
    }

    private static async Task FetchAndPostNotificationsAsync()
    {
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
        // while the baseline still advances past them. Favorite friend and
        // emotion notifications are filtered out when the user disabled them.
        // The known-id set (bounded at 50) keeps already-posted notifications
        // from being posted again when the baseline fell out of the window.
        var isFavoriteFriendNotificationEnabled = Configuration.GetValue<bool?>(FavoriteFriendNotificationEnabledKey) ?? true;
        var isEmotionNotificationEnabled = Configuration.GetValue<bool?>(EmotionNotificationEnabledKey) ?? true;
        var knownIds = new HashSet<string>(GetKnownIds(KnownNotificationIdsKey));
        var newNotifications = new List<Notification>();
        var postIds = new HashSet<string>();
        foreach (var notification in notifications)
        {
            if (notification.id == storedLatestId) break;
            if (!notification.is_new) continue;
            if (IsFavoriteFriendNotification(notification) && !isFavoriteFriendNotificationEnabled) continue;
            if (IsEmotionNotification(notification) && !isEmotionNotificationEnabled) continue;
            if (notification.id == null || !knownIds.Add(notification.id)) continue;
            if (TryGetPostId(notification.scheme, out var postId)) postIds.Add(postId);
            newNotifications.Add(notification);
        }

        if (newNotifications.Count > 0)
        {
            foreach (var notification in newNotifications) PostNotification(notification);
            SaveKnownIds(KnownNotificationIdsKey, [.. knownIds]);
        }

        if (postIds.Count > 0) await RefreshActivePostsAsync(postIds);

        Preferences.Set(LatestNotificationIdKey, latestId);
    }

    /// <summary>
    /// Polls the Kakao Story mail list and posts a local notification for each
    /// new unread received mail. The notification fetch API does not carry mail
    /// events, so mails are watched separately with their own baseline. Only
    /// received (type == "receive") and unread (read_at == null) mails newer than
    /// the stored baseline are posted; the baseline advances past read and sent
    /// mails as well. The user can disable it from the settings page.
    /// </summary>
    private static async Task FetchAndPostMailsAsync()
    {
        if ((Configuration.GetValue<bool?>(IsEnabledKey) ?? true) == false || (Configuration.GetValue<bool?>(MailNotificationEnabledKey) ?? true) == false) return;

        var mails = await KakaoStoryApiHandler.GetMails();
        if (mails == null || mails.Count == 0) return;

        var latestId = mails[0].id;
        if (string.IsNullOrEmpty(latestId)) return;

        var storedLatestId = Preferences.Get(LatestMailIdKey, string.Empty);
        if (string.IsNullOrEmpty(storedLatestId))
        {
            // First poll ever (no baseline recorded yet): only record the baseline
            // so fresh installs do not blast every past mail at once.
            Preferences.Set(LatestMailIdKey, latestId);
            return;
        }

        if (storedLatestId == latestId) return; // Nothing new.

        // The list is newest-first: everything newer than the stored baseline is
        // new. Read mails (read_at != null) and sent mails (type != "receive")
        // are skipped, while the baseline still advances past them. The known-id
        // set (bounded at 50) keeps already-posted mails from being posted again
        // when the baseline fell out of the window.
        var knownIds = new HashSet<string>(GetKnownIds(KnownMailIdsKey));
        var newMails = new List<MailData.Mail>();
        foreach (var mail in mails)
        {
            if (mail.id == storedLatestId) break;
            if (mail.type != "receive") continue;
            if (mail.read_at != null) continue;
            if (mail.id == null || !knownIds.Add(mail.id)) continue;
            newMails.Add(mail);
        }

        if (newMails.Count > 0)
        {
            foreach (var mail in newMails) PostMail(mail);
            SaveKnownIds(KnownMailIdsKey, [.. knownIds]);
        }

        Preferences.Set(LatestMailIdKey, latestId);
    }

    private static bool IsFavoriteFriendNotification(Notification notification) => notification.decorators is { Count: > 0 } && notification.decorators[0].text?.StartsWith("관심친구") == true;

    private static bool IsEmotionNotification(Notification notification) => notification.emotion != null;

    private static void PostNotification(Notification notification)
    {
#if ANDROID
        // A transient Notify failure (e.g. channel disabled) must not abort the
        // cycle before the baseline advances, or the same notification would be
        // posted again on the next poll.
        try { KakaoStoryNotificationPoster.Post(notification); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story notification post failed: {exception.Message}"); }
#elif IOS
        try { KakaoStoryNotificationPoster.Post(notification); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story notification post failed: {exception.Message}"); }
#endif
    }

    private static void PostMail(MailData.Mail mail)
    {
#if ANDROID
        try { KakaoStoryNotificationPoster.PostMail(mail); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story mail notification post failed: {exception.Message}"); }
#elif IOS
        try { KakaoStoryNotificationPoster.PostMail(mail); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story mail notification post failed: {exception.Message}"); }
#endif
    }

    /// <summary>
    /// Posts the "Kakao Story login expired" notification at most once per expired
    /// session. The flag is cleared again once a login succeeds (see
    /// ResetSessionExpiredNotification). The user can disable it from the settings page.
    /// </summary>
    private static void HandleSessionExpired()
    {
        if ((Configuration.GetValue<bool?>(SessionExpiredNotificationEnabledKey) ?? true) == false) return;
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
        try { KakaoStoryNotificationPoster.PostSessionExpired(); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story session expired notification post failed: {exception.Message}"); }
#elif IOS
        try { KakaoStoryNotificationPoster.PostSessionExpired(); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story session expired notification post failed: {exception.Message}"); }
#endif
    }

    private static List<string> GetKnownIds(string key)
    {
        var raw = Preferences.Get(key, string.Empty);
        return string.IsNullOrEmpty(raw) ? [] : [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static void SaveKnownIds(string key, List<string> ids)
    {
        if (ids.Count > MaxKnownIds) ids = ids[..MaxKnownIds];
        Preferences.Set(key, string.Join(',', ids));
    }

    /// <summary>
    /// Extracts the activity id from a post notification scheme
    /// (e.g. kakaostory://activities/{activityId}?profile_id={profileId}).
    /// The profile_id guard keeps schemes that carry an activity id without the
    /// profile context from being treated as post notifications.
    /// </summary>
    private static bool TryGetPostId(string scheme, out string postId)
    {
        postId = null;
        if (string.IsNullOrEmpty(scheme)) return false;
        if (!scheme.Contains("?profile_id=") || !scheme.Contains("activities/")) return false;
        postId = scheme.Split(new[] { "activities/" }, StringSplitOptions.None)[1];
        var queryIndex = postId.IndexOf('?');
        if (queryIndex >= 0) postId = postId[..queryIndex];
        return !string.IsNullOrEmpty(postId);
    }

    /// <summary>
    /// Refreshes active Kakao Story post view models via the same
    /// ValueChangedMessage&lt;PostData&gt; broadcast used by RefreshAsync. Per-post
    /// try-catch keeps a single failure from aborting the rest of the poll cycle.
    /// </summary>
    private static async Task RefreshActivePostsAsync(IEnumerable<string> postIds)
    {
        foreach (var postId in postIds)
        {
            try
            {
                var post = await KakaoStoryApiHandler.GetPost(postId);
                if (post == null) continue;
                MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post)));
            }
            catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story post refresh failed: {exception.Message}"); }
        }
    }
}
