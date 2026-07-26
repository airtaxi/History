using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace History.MobileClient;

// Custom ShellRenderer that exposes the native iOS tab bar height and keeps
// the tab bar translucent so Liquid Glass on iOS 26 can show content beneath it.
public class CustomShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker() => new CustomTabBarAppearanceTracker();
}
