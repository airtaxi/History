using System.Security.Cryptography;
using System.Text;
using Foundation;
using UIKit;
using UserNotifications;
using KakaoMail = History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.MailData.Mail;

namespace History.MobileClient;

/// <summary>
/// Posts a Kakao Story mail as a local notification through the
/// UNUserNotificationCenter (the iOS mirror of the Android poster; Firebase is
/// not involved here). Tapping it opens the mail detail via the
/// kakaostory://messages/ scheme stored in the user info. Keyed by the mail id
/// so a newer mail from the same sender replaces the older one instead of
/// stacking.
/// </summary>
public static class KakaoStoryNotificationPoster
{
    private const string MailNotificationIdPrefix = "kakaostory-mail-";
    public const string SchemeExtraKey = "KakaoStoryNotificationScheme";

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

    private static bool IsAuthorizationGranted(UNNotificationSettings settings)
        => settings.AuthorizationStatus == UNAuthorizationStatus.Authorized
            || settings.AuthorizationStatus == UNAuthorizationStatus.Provisional;

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
