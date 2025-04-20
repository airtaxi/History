using Foundation;
using Google.SignIn;
using UIKit;

namespace History.MobileClient
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        {
            SignIn.SharedInstance.HandleUrl(url);
            return true;
        }
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
