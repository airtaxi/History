using History.Commons;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.KakaoStory;

/// <summary>
/// Polls the Kakao Story mail list and raises local notifications for new items.
/// The newest mail id is persisted as a baseline; any item newer than that
/// baseline (the API list is newest-first and capped at 30) is posted. A bounded
/// set of already-posted ids (50) guards against re-posting when the baseline
/// falls out of the 30-item window. Used by the Android background JobService
/// (15-minute cadence) and the iOS BGAppRefreshTask. Notification polling is
/// owned by the server (FCM push), so this poller only watches mails. 401
/// responses never open the login modal from here; the poll cycle just ends
/// silently and the saved tokens are revalidated on the next cycle. The tab bar
/// badges are not this poller's concern: they are kept up to date by
/// <see cref="TabBarBadgePoller"/> and the list pages.
/// </summary>
public static class KakaoStoryMailPoller
{
    private const string LatestMailIdKey = "KakaoStoryLatestMailId";
    private const string KnownMailIdsKey = "KakaoStoryKnownMailIds";
    private const int MaxKnownIds = 50;
    private const bool IsPollLoggingEnabled = true;
    private const string IsEnabledKey = "KakaoStoryNotificationEnabled";
    private const string MailNotificationEnabledKey = "KakaoStoryMailNotificationEnabled";

    private static readonly SemaphoreSlim s_pollSemaphore = new(1, 1);

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
    /// Single poll cycle shared by the Android JobService and the iOS background
    /// refresh task. Reentrancy is guarded so concurrent cycles never overlap.
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
        try { await FetchAndPostMailsAsync(); }
        finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
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
}
