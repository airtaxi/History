#if IOS
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;
using UIKit;

#pragma warning disable CA1422 // Type or member is obsolete — UIApplication.SharedApplication.Windows / SetStatusBarStyle are deprecated but still required for status bar customization on iOS.

namespace History.MobileClient.Behaviors;

public partial class StatusBarBehavior
{
    static readonly IntPtr StatusBarTag = new(38482);

    static partial void PlatformSetColor(Color color)
    {
        var uiColor = color.ToPlatform();

        foreach (var window in UIApplication.SharedApplication.Windows)
        {
            var statusBarFrame = window.WindowScene?.StatusBarManager?.StatusBarFrame;
            if (statusBarFrame is null) continue;

            var statusBar = window.ViewWithTag(StatusBarTag) ?? [with(statusBarFrame.Value)];
            statusBar.Tag = StatusBarTag;
            statusBar.BackgroundColor = uiColor;
            statusBar.TintColor = uiColor;

            // Remove any previously added status bar subviews to avoid duplicates.
            foreach (var subview in window.Subviews.Where(view => view.Tag == StatusBarTag).ToList()) subview.RemoveFromSuperview();

            window.AddSubview(statusBar);

            TryUpdateStatusBarAppearance(window);
        }
    }

    static partial void PlatformSetTheme(StatusBarTheme theme)
    {
        var uiStyle = theme switch
        {
            StatusBarTheme.Light => UIStatusBarStyle.DarkContent,
            StatusBarTheme.Dark => UIStatusBarStyle.LightContent,
            _ => UIStatusBarStyle.Default
        };

        UIApplication.SharedApplication.SetStatusBarStyle(uiStyle, false);
        TryUpdateStatusBarAppearance();
    }

    static bool TryUpdateStatusBarAppearance()
    {
        var didUpdateAllStatusBars = true;
        foreach (var window in UIApplication.SharedApplication.Windows) didUpdateAllStatusBars &= TryUpdateStatusBarAppearance(window);
        return didUpdateAllStatusBars;
    }

    static bool TryUpdateStatusBarAppearance(UIWindow window)
    {
        var viewController = window?.RootViewController ?? WindowStateManager.Default.GetCurrentUIViewController();
        if (viewController is null)
        {
            Trace.WriteLine("Unable to update Status Bar Appearance because Current UIViewController is null");
            return false;
        }

        while (viewController.PresentedViewController is not null) viewController = viewController.PresentedViewController;

        viewController.SetNeedsStatusBarAppearanceUpdate();
        return true;
    }
}
#endif