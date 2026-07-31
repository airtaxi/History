#if IOS
using Foundation;
using UIKit;
using Plugin.Firebase.Core.Platforms.iOS;
using Plugin.Firebase.CloudMessaging;
using History.Uno.Services;

namespace History.Uno.iOS;

// Skia rendering: App does not derive from UIApplicationDelegate, so a custom
// UnoUIApplicationDelegate is used to hook into iOS lifecycle for Firebase init.
public class AppDelegate : global::Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        CrossFirebase.Initialize();
        FirebaseCloudMessagingImplementation.Initialize();

        // Subscribe to FCM notification events (shared handler)
        CrossFirebaseCloudMessaging.Current.NotificationTapped += NotificationHandler.OnNotificationTapped;
        CrossFirebaseCloudMessaging.Current.NotificationReceived += NotificationHandler.OnNotificationReceived;

        return base.FinishedLaunching(application, launchOptions);
    }
}
#endif