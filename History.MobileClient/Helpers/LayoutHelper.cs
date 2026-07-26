#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace History.MobileClient.Helpers;

public static class LayoutHelper
{
    public static double GetSafeAreaTopHeight()
    {
        double safeAreaTopHeight = 0;

        if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
        {
            UIWindow window = UIApplication.SharedApplication.Delegate.GetWindow();

            safeAreaTopHeight = window != null
                ? window.SafeAreaInsets.Top
                : 0;
        }

        return safeAreaTopHeight;
    }

    public static double GetStatusBarHeight() => UIApplication.SharedApplication.StatusBarFrame.Height;

    // Returns the full tab bar frame height, including the home indicator inset.
    // Prefers the value captured by CustomTabBarAppearanceTracker, then falls back
    // to the live window RootViewController, then to the UIKit default (49pt).
    public static double GetTabBarHeight()
    {
        if (CustomTabBarAppearanceTracker.TabBarHeight > 0) return CustomTabBarAppearanceTracker.TabBarHeight;

        var window = UIApplication.SharedApplication.Delegate.GetWindow();
        if (window?.RootViewController is UITabBarController tabBarController)
        {
            var height = tabBarController.TabBar.Frame.Height;
            if (height > 0) return height;
        }

        return 49d;
    }
}
#else
public static class LayoutHelper
{
    public static double GetSafeAreaTopHeight() => 0;

    public static double GetStatusBarHeight() => 0;
}

#endif