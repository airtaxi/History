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

    private const string PendingSchemeKey = "KakaoStoryNotificationSchemePending";

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
            HandleScheme(scheme);
            completionHandler();
            return;
        }

        // FCM notification tap: preserve the plugin's tap event. The completion
        // handler must still be called when the plugin delegate is unreachable.
        if (s_firebaseDelegate.Value is { } firebaseDelegate) firebaseDelegate.DidReceiveNotificationResponse(center, response, completionHandler);
        else completionHandler();
    }

    /// <summary>
    /// Replays a notification scheme deferred during a cold start (the app
    /// shell was not up yet). Called from LoginPage.AfterLogin, mirroring the
    /// FCM PushData preference pattern.
    /// </summary>
    public static void ReplayPendingScheme()
    {
        var scheme = Preferences.Get(PendingSchemeKey, null);
        if (string.IsNullOrEmpty(scheme)) return;
        Preferences.Set(PendingSchemeKey, null);
        HandleScheme(scheme);
    }

    private static string GetScheme(NSDictionary userInfo)
    {
        if (userInfo == null) return null;
        var value = userInfo[new NSString(KakaoStoryNotificationPoster.SchemeExtraKey)] as NSString;
        return value?.ToString();
    }

    private static void HandleScheme(string scheme)
    {
        if (!AppShell.IsLoaded)
        {
            // Cold start: the root page is still the login page and the shell
            // would discard the pushed page on login. Defer the navigation.
            Preferences.Set(PendingSchemeKey, scheme);
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Post notification: the scheme contains the activity id after "activities/".
                if (scheme.Contains("?profile_id=") && scheme.Contains("activities/"))
                {
                    var postId = scheme.Split(new[] { "activities/" }, StringSplitOptions.None)[1];
                    var post = await KakaoStory.KakaoStoryApiHandler.GetPost(postId);
                    if (post == null) return;

                    var postViewModel = new ViewModels.KakaoPostViewModel(post, Enums.PostType.Unwrapped);
                    await App.PushAsync(new Pages.PostPage(postViewModel));
                }
                // Profile notification: the scheme is a kakaostory:// deep link to the profile.
                else if (scheme.Contains("kakaostory://profiles/"))
                {
                    var profileId = scheme.Replace("kakaostory://profiles/", "");
                    if (string.IsNullOrEmpty(profileId)) return;

                    await App.PushAsync(new Pages.UserPage(profileId, true));
                }
            }
            catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story notification navigation failed: {exception.Message}"); }
        });
    }
}
