using Foundation;
using Google.SignIn;
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
        NativeMedia.Platform.Init(GetTopViewController);
        return base.FinishedLaunching(app, options);
    }

    //This is a sample method, replace it with what you need
    public UIViewController GetTopViewController()
    {
        var vc = UIApplication.SharedApplication.KeyWindow.RootViewController;

        if (vc is UINavigationController navController)
            vc = navController.ViewControllers.Last();

        return vc;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
