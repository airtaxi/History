#if ANDROID
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using PlatformColor = Android.Graphics.Color;

#pragma warning disable CA1422, CA1416 // Type or member is obsolete / Validate platform compatibility — Window.SetStatusBarColor is deprecated on API 35+ but still works and avoids the foldable overlay sizing bug in CommunityToolkit.

namespace History.MobileClient.Behaviors;

public partial class StatusBarBehavior
{
    static partial void PlatformSetColor(Color color)
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window is null) return;

        var platformColor = color.ToPlatform();

        // Use the native Window.SetStatusBarColor on all API levels.
        // The CommunityToolkit overlay-based approach for API 35+ causes foldable
        // screen resize issues because the overlay height is stale after configuration
        // changes. Setting the window status bar color directly avoids that entirely.
        window.SetStatusBarColor(platformColor);

        // Keep the window layout flags in sync with transparency so edge-to-edge
        // behavior matches the requested color.
        if (platformColor == PlatformColor.Transparent)
        {
            window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
            WindowCompat.SetDecorFitsSystemWindows(window, false);
        }
        else
        {
            window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
            WindowCompat.SetDecorFitsSystemWindows(window, true);
        }
    }

    static partial void PlatformSetTheme(StatusBarTheme theme)
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window is null) return;

        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is null) return;

        // AppearanceLightStatusBars = true  → dark icons (for light backgrounds, StatusBarTheme.Light)
        // AppearanceLightStatusBars = false → light icons (for dark backgrounds, StatusBarTheme.Dark)
        controller.AppearanceLightStatusBars = theme is StatusBarTheme.Light;
    }
}
#endif