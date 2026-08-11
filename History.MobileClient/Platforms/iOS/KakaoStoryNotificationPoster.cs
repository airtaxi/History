using System.Security.Cryptography;
using System.Text;
using Foundation;
using UIKit;
using UserNotifications;
using KakaoMail = History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.MailData.Mail;
using KakaoNotification = History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.Notification;

namespace History.MobileClient;

/// <summary>
/// Posts a Kakao Story notification as a local notification through the
/// UNUserNotificationCenter (the iOS mirror of the Android poster; Firebase is
/// not involved here). Tapping it routes to the notification target (post or
/// profile) via the scheme stored in the user info. Notifications are keyed by
/// their scheme, so a newer notification for the same target replaces the older
/// one instead of stacking.
/// </summary>
public static class KakaoStoryNotificationPoster
{
    private const string NotificationIdPrefix = "kakaostory-notification-";
    private const string MailNotificationIdPrefix = "kakaostory-mail-";
    private const string SessionExpiredNotificationId = "kakaostory-session-expired";
    public const string SchemeExtraKey = "KakaoStoryNotificationScheme";

    public static void Post(KakaoNotification notification)
    {
        UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
        {
            if (!IsAuthorizationGranted(settings)) return;

            var title = notification.message ?? string.Empty;
            var contentText = notification.content ?? "내용 없음";

            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = contentText,
                Sound = UNNotificationSound.Default,
            };
            if (!string.IsNullOrEmpty(notification.scheme))
                content.UserInfo = NSDictionary.FromObjectAndKey(new NSString(notification.scheme), new NSString(SchemeExtraKey));

            var request = UNNotificationRequest.FromIdentifier(GetNotificationId(notification), content, null);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
            {
                if (error != null) System.Diagnostics.Debug.WriteLine($"Kakao Story notification post failed: {error.LocalizedDescription}");
            });
        });
    }

    /// <summary>
    /// Posts a Kakao Story mail notification (the notification fetch API does not
    /// carry mail events, so mails are watched separately by the poller). Tapping
    /// it opens the mail detail via the kakaostory://messages/ scheme stored in
    /// the user info. Keyed by the mail id so a newer mail from the same sender
    /// replaces the older one instead of stacking.
    /// </summary>
    public static void PostMail(KakaoMail mail)
    {
        UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
        {
            if (!IsAuthorizationGranted(settings)) return;

            var scheme = $"kakaostory://messages/{mail.id}";
            var title = $"{mail.sender?.display_name ?? "알 수 없음"}님이 쪽지를 보냈습니다";
            var contentText = mail.summary ?? "내용 없음";

            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = contentText,
                Sound = UNNotificationSound.Default,
            };
            content.UserInfo = NSDictionary.FromObjectAndKey(new NSString(scheme), new NSString(SchemeExtraKey));

            var request = UNNotificationRequest.FromIdentifier(GetMailNotificationId(mail.id), content, null);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
            {
                if (error != null) System.Diagnostics.Debug.WriteLine($"Kakao Story mail notification post failed: {error.LocalizedDescription}");
            });
        });
    }

    /// <summary>
    /// Posts the "Kakao Story login expired" notification. Tapping it only opens
    /// the app (no scheme in the user info), where the user can re-login.
    /// </summary>
    public static void PostSessionExpired()
    {
        UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
        {
            if (!IsAuthorizationGranted(settings)) return;

            var content = new UNMutableNotificationContent
            {
                Title = "카카오스토리 로그인 만료",
                Body = "카카오스토리 로그인이 만료되었습니다. 앱을 열어 다시 로그인해주세요.",
                Sound = UNNotificationSound.Default,
            };

            var request = UNNotificationRequest.FromIdentifier(SessionExpiredNotificationId, content, null);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
            {
                if (error != null) System.Diagnostics.Debug.WriteLine($"Kakao Story session expired notification post failed: {error.LocalizedDescription}");
            });
        });
    }

    private static bool IsAuthorizationGranted(UNNotificationSettings settings)
        => settings.AuthorizationStatus == UNAuthorizationStatus.Authorized
            || settings.AuthorizationStatus == UNAuthorizationStatus.Provisional;

    /// <summary>
    /// Derives a stable notification identifier from the scheme so replacements
    /// survive process restarts (string.GetHashCode is randomized per process).
    /// </summary>
    private static string GetNotificationId(KakaoNotification notification)
    {
        var key = notification.scheme;
        if (string.IsNullOrEmpty(key)) key = notification.key;
        if (string.IsNullOrEmpty(key)) key = notification.id;
        if (string.IsNullOrEmpty(key)) return NotificationIdPrefix + "default";

        return NotificationIdPrefix + GetHash(key);
    }

    private static string GetMailNotificationId(string mailId)
    {
        if (string.IsNullOrEmpty(mailId)) return MailNotificationIdPrefix + "default";

        return MailNotificationIdPrefix + GetHash(mailId);
    }

    private static string GetHash(string key)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hashBytes, 0, 16).ToLowerInvariant();
    }
}
