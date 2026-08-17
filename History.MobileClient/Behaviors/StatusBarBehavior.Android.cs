#if ANDROID
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using PlatformColor = Android.Graphics.Color;

#pragma warning disable CA1422, CA1416 // Type or member is obsolete / Validate platform compatibility — Window.SetStatusBarColor is deprecated on API 35+ but still works on lower API levels; the 35+ path uses a DecorView overlay.

namespace History.MobileClient.Behaviors;

public partial class StatusBarBehavior
{
    const string statusBarOverlayTag = "StatusBarOverlay";

    // Resolved lazily so we don't need an Activity instance at static-init time.
    static int statusBarHeightResourceId;

    static readonly HashSet<Android.Views.View> attachedDecorViews = [];

    static partial void PlatformSetColor(Color color)
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window is null) return;

        var platformColor = color.ToPlatform();

        if (OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            // API 35+ enforces edge-to-edge regardless of decor flag toggles, so
            // Window.SetStatusBarColor becomes a no-op. Paint the status bar with a
            // DecorView overlay whose height is re-measured on every layout change
            // (see OnAttachedToPlatform). Keeping the height in sync with the real
            // inset is what prevents the foldable "striping" — a stale overlay
            // height leaves the page background showing through the gap.
            ApplyStatusBarOverlay(window, platformColor);
        }
        else
        {
            window.SetStatusBarColor(platformColor);
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

    static partial void OnAttachedToPlatform(Page page, object platformView)
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window?.DecorView is not Android.Views.View decorView) return;

        // Re-measure the overlay height whenever the DecorView re-lays out
        // (foldable unfold/fold, rotation, tab switch). This closes the gap that
        // could otherwise expose the page background as stripes. Many behaviors
        // (one per Shell page) attach to the same shared DecorView, so guard with a
        // set to avoid registering duplicate layout listeners.
        if (attachedDecorViews.Add(decorView))
        {
            decorView.AddOnLayoutChangeListener(new StatusBarLayoutChangeListener());
            SyncStatusBarOverlay();
        }
    }

    static partial void OnDetachedFromPlatform(Page page, object platformView)
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window?.DecorView is not Android.Views.View decorView) return;

        // Only remove the listener once all attached behaviors are gone from this
        // DecorView, so an early-detached behavior does not unregister the shared
        // listener for the pages that are still attached.
        attachedDecorViews.Remove(decorView);
        if (attachedDecorViews.Count == 0) decorView.RemoveOnLayoutChangeListener(StatusBarLayoutChangeListener.Instance);
    }

    sealed class StatusBarLayoutChangeListener : Java.Lang.Object, Android.Views.View.IOnLayoutChangeListener
    {
        public static readonly StatusBarLayoutChangeListener Instance = new();

        // Foldable screen transitions can change the status bar inset in steps; a
        // layout pass settles on the final value, so re-measure here rather than
        // relying on a single DisplayInfo event that may arrive stale.
        public void OnLayoutChange(Android.Views.View view, int left, int top, int right, int bottom, int oldLeft, int oldTop, int oldRight, int oldBottom)
        {
            MainThread.BeginInvokeOnMainThread(SyncStatusBarOverlay);
        }
    }

    static void ApplyStatusBarOverlay(Android.Views.Window window, PlatformColor platformColor)
    {
        if (window.DecorView?.RootView is not Android.Views.ViewGroup decorGroup) return;

        var overlay = decorGroup.FindViewWithTag(statusBarOverlayTag);
        if (overlay is null)
        {
            overlay = new Android.Views.View(Platform.CurrentActivity)
            {
                LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, GetStatusBarHeight(window))
                {
                    Gravity = GravityFlags.Top
                },
                Tag = statusBarOverlayTag
            };
            overlay.SetZ(0);
            decorGroup.AddView(overlay);
        }

        overlay.SetBackgroundColor(platformColor);
        SyncStatusBarOverlay();
    }

    static void SyncStatusBarOverlay()
    {
        if (Platform.CurrentActivity is not Activity activity) return;
        var window = activity.Window;
        if (window?.DecorView?.RootView is not Android.Views.ViewGroup decorGroup) return;

        var overlay = decorGroup.FindViewWithTag(statusBarOverlayTag);
        if (overlay?.LayoutParameters is not Android.Widget.FrameLayout.LayoutParams layoutParams) return;

        var height = GetStatusBarHeight(window);
        if (layoutParams.Height != height)
        {
            layoutParams.Height = height;
            layoutParams.Gravity = GravityFlags.Top;
            layoutParams.Width = ViewGroup.LayoutParams.MatchParent;
            overlay.LayoutParameters = layoutParams;
        }
    }

    static int GetStatusBarHeight(Android.Views.Window window)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(36))
        {
            var insets = window.DecorView?.RootWindowInsets;
            return insets?.GetInsets(WindowInsets.Type.StatusBars()).Top ?? 0;
        }
        else
        {
            // API 35 only — insets are unavailable through RootWindowInsets, use the framework resource.
            if (statusBarHeightResourceId == 0 && Platform.CurrentActivity is Activity activity)
                statusBarHeightResourceId = activity.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;

            return statusBarHeightResourceId != 0 && Platform.CurrentActivity is Activity currentActivity
                ? currentActivity.Resources?.GetDimensionPixelSize(statusBarHeightResourceId) ?? 0
                : 0;
        }
    }
}
#endif