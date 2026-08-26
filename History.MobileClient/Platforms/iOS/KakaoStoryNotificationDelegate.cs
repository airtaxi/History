using Foundation;
using Plugin.Firebase.CloudMessaging;
using UIKit;
using UserNotifications;

namespace History.MobileClient;

/// <summary>
/// UNUserNotificationCenter delegate that owns the center for the app. The
/// Firebase plugin sets itself as the delegate when it initializes (WillFinish
/// Launching); this delegate replaces it afterwards and forwards every callback
/// for non-Kakao notifications back to the plugin, so the existing FCM push
/// behavior (foreground banner, tap events) keeps working.
/// </summary>
public class KakaoStoryNotificationDelegate : UNUserNotificationCenterDelegate
{
    // The Firebase plugin's implementation object is the previous delegate; it
    // implements IUNUserNotificationCenterDelegate (not the UNUserNotification
    // CenterDelegate subclass), so callbacks are forwarded through the interface.
    private static readonly Lazy<IUNUserNotificationCenterDelegate> s_firebaseDelegate = new(
        () => CrossFirebaseCloudMessaging.Current as IUNUserNotificationCenterDelegate);

    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        // The session-expired notification has no UserInfo, so it must be
        // null-checked before reading.
        if (notification.Request.Content.UserInfo?.ContainsKey(new NSString(KakaoStoryNotificationPoster.SchemeExtraKey)) == true)
        {
            // Our own Kakao Story notification: show a foreground banner.
            var options = UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Sound;
            completionHandler(options);
            return;
        }

        // FCM notification: preserve the plugin's presentation behavior. The
        // completion handler must still be called when the plugin delegate is
        // unreachable, so fall back to dismissing silently.
        if (s_firebaseDelegate.Value is { } firebaseDelegate) firebaseDelegate.WillPresentNotification(center, notification, completionHandler);
        else completionHandler(new UNNotificationPresentationOptions());
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        var scheme = GetScheme(response.Notification.Request.Content.UserInfo);
        if (!string.IsNullOrEmpty(scheme))
        {
            _ = App.HandleKakaoStoryNotificationAsync(scheme);
            completionHandler();
            return;
        }

        // FCM notification tap: preserve the plugin's tap event. The completion
        // handler must still be called when the plugin delegate is unreachable.
        if (s_firebaseDelegate.Value is { } firebaseDelegate) firebaseDelegate.DidReceiveNotificationResponse(center, response, completionHandler);
        else completionHandler();
    }

    private static string GetScheme(NSDictionary userInfo)
    {
        if (userInfo == null) return null;
        var value = userInfo[new NSString(KakaoStoryNotificationPoster.SchemeExtraKey)] as NSString;
        return value?.ToString();
    }
}
