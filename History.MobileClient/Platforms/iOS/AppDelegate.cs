using CommunityToolkit.Mvvm.Messaging;
using Foundation;
using Google.SignIn;
using History.MobileClient.DataTypes;
using UIKit;

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
