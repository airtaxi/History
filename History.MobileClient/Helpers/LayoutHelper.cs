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
}
#else
public static class LayoutHelper
{
    public static double GetSafeAreaTopHeight() => 0;

    public static double GetStatusBarHeight() => 0;
}

#endif