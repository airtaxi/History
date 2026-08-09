using CommunityToolkit.Mvvm.Messaging;
using Foundation;
using Google.SignIn;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using UIKit;
using UserNotifications;

namespace History.MobileClient;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        SignIn.SharedInstance.HandleUrl(url);
        return true;
    }

    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        var result = base.FinishedLaunching(app, options);

        NSNotificationCenter.DefaultCenter.AddObserver(
            UIKeyboard.WillShowNotification,
            OnKeyboardWillShow);

        NSNotificationCenter.DefaultCenter.AddObserver(
            UIKeyboard.WillHideNotification,
            OnKeyboardWillHide);

        // The Firebase plugin sets itself as the notification center delegate
        // during WillFinishLaunching; this delegate takes ownership afterwards
        // and forwards non-Kakao callbacks back to the plugin.
        UNUserNotificationCenter.Current.Delegate = new KakaoStoryNotificationDelegate();

        // The background task handler must be registered before the app finishes
        // launching. The refresh is scheduled lazily when the app enters the
        // background, so no request is pending unless the app was used.
        KakaoStoryBackgroundRefresh.Register();

        return result;
    }

    private void OnKeyboardWillShow(NSNotification notification)
    {
        if (notification.UserInfo != null)
        {
            var keyboardFrame = ((NSValue)notification.UserInfo[UIKeyboard.FrameEndUserInfoKey]).CGRectValue;
            var keyboardHeight = keyboardFrame.Height;

            WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(keyboardHeight));
        }
    }

    private void OnKeyboardWillHide(NSNotification notification) => WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(0));

    public override void WillTerminate(UIApplication application)
    {
        // 알림 구독 해제
        NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        base.WillTerminate(application);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
