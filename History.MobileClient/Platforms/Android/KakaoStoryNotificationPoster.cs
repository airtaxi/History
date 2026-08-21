using System.Security.Cryptography;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using KakaoMail = History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.MailData.Mail;

namespace History.MobileClient;

/// <summary>
/// Posts a Kakao Story mail as a local notification on the existing push
/// channel ({PackageName}.push). Tapping it opens the mail detail via the
/// kakaostory://messages/ scheme extra handled by MainActivity. Keyed by the
/// mail id so a newer mail from the same sender replaces the older one instead
/// of stacking.
/// </summary>
public static class KakaoStoryNotificationPoster
{
    private const int NotificationIdBase = 9002;
    public const string SchemeExtraKey = "KakaoStoryNotificationScheme";

    /// <summary>
    /// Posts a Kakao Story mail notification (the notification fetch API does not
    /// carry mail events, so mails are watched separately by the poller). Tapping
    /// it opens the mail detail via the kakaostory://messages/ scheme handled by
    /// App.HandleKakaoStoryNotificationAsync. Keyed by the mail id so a newer mail
    /// from the same sender replaces the older one instead of stacking.
    /// </summary>
    public static void PostMail(KakaoMail mail)
    {
        var context = Platform.AppContext;
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted) return;

        var scheme = $"kakaostory://messages/{mail.id}";
        var notificationId = GetNotificationIdFromScheme(scheme);

        var title = $"{mail.sender?.display_name ?? "알 수 없음"}님이 쪽지를 보냈습니다";
        var contentText = mail.summary ?? "내용 없음";

        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        intent.PutExtra(SchemeExtraKey, scheme);

        var pendingIntent = PendingIntent.GetActivity(context, notificationId, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(context, $"{context.PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(contentText)
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent);

        NotificationManagerCompat.From(context).Notify(notificationId, builder.Build());
    }

    /// <summary>
    /// Derives a stable notification id from the scheme so replacements survive
    /// process restarts (string.GetHashCode is randomized per process).
    /// </summary>
    private static int GetNotificationIdFromScheme(string scheme)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(scheme));
        return NotificationIdBase + (BitConverter.ToInt32(hashBytes, 0) & 0x7FFFFFFF) % 100000;
    }
}
