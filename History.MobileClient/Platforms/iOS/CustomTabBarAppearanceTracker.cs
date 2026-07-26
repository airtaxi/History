using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using Microsoft.Maui.Controls.Platform.Compatibility;
using UIKit;

namespace History.MobileClient;

public class CustomTabBarAppearanceTracker : ShellTabBarAppearanceTracker
{
    private static UITabBarController s_controller;
    // Captured during SetAppearance once the tab bar has been laid out.
    // Includes the home indicator inset (e.g. 83pt on notched devices, 49pt on SE).
    public static double TabBarHeight { get; private set; }

    // Delay one frame to apply TabBarHeight
    static CustomTabBarAppearanceTracker()
    {
        UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
        UIDevice.Notifications.ObserveOrientationDidChange((s, e) =>
        {
            if (s_controller is null) return;

            Application.Current.Dispatcher.Dispatch(() =>
            {
                TabBarHeight = s_controller.TabBar.Frame.Height;
                WeakReferenceMessenger.Default.Send(new TabBarHeightChangedMessage(TabBarHeight));
            });
        });
    }

    public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
    {
        if (controller is not null) s_controller = controller;

        base.SetAppearance(controller, appearance);

        // Ensure translucency so Liquid Glass can blur content scrolling under it.
        controller.TabBar.Translucent = true;

        TabBarHeight = controller.TabBar.Frame.Height;
    }
}