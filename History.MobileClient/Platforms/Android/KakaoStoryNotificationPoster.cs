using System.Security.Cryptography;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using KakaoNotification = History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.Notification;

namespace History.MobileClient;

/// <summary>
/// Posts a Kakao Story notification as a local notification on the existing
/// push channel ({PackageName}.push). Tapping it routes to the notification
/// target (post or profile) via the scheme extra handled by MainActivity.
/// Notifications are keyed by their scheme, so a newer notification for the
/// same target replaces the older one instead of stacking.
/// </summary>
public static class KakaoStoryNotificationPoster
{
    private const int NotificationIdBase = 9002;
    private const int SessionExpiredNotificationId = 9001;
    public const string SchemeExtraKey = "KakaoStoryNotificationScheme";

    public static void Post(KakaoNotification notification)
    {
        var context = Platform.AppContext;
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted) return;

        var notificationId = GetNotificationId(notification);

        var title = notification.message ?? string.Empty;
        var contentText = notification.content ?? "내용 없음";

        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        if (!string.IsNullOrEmpty(notification.scheme)) intent.PutExtra(SchemeExtraKey, notification.scheme);

        // The request code must differ per scheme: PendingIntent equality ignores
        // extras, so sharing a request code would reuse the first PendingIntent and
        // overwrite its scheme with the latest one.
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
    /// Posts the "Kakao Story login expired" notification. Tapping it only opens
    /// the app (no scheme extra), where the user can re-login.
    /// </summary>
    public static void PostSessionExpired()
    {
        var context = Platform.AppContext;
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted) return;

        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntent = PendingIntent.GetActivity(context, SessionExpiredNotificationId, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(context, $"{context.PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle("카카오스토리 로그인 만료")
            .SetContentText("카카오스토리 로그인이 만료되었습니다. 앱을 열어 다시 로그인해주세요.")
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent);

        NotificationManagerCompat.From(context).Notify(SessionExpiredNotificationId, builder.Build());
    }

    /// <summary>
    /// Derives a stable notification id from the scheme so replacements survive
    /// process restarts (string.GetHashCode is randomized per process).
    /// </summary>
    private static int GetNotificationId(KakaoNotification notification)
    {
        var key = notification.scheme;
        if (string.IsNullOrEmpty(key)) key = notification.key;
        if (string.IsNullOrEmpty(key)) key = notification.id;
        if (string.IsNullOrEmpty(key)) return NotificationIdBase;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return NotificationIdBase + (BitConverter.ToInt32(hashBytes, 0) & 0x7FFFFFFF) % 100000;
    }
}
