using Android.Content;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace History.MobileClient;

// Re-raises Shell.Navigating when the already-selected bottom tab is tapped again
// on Android (community-verified workaround for dotnet/maui issue #15301, mirroring iOS behavior).
public class AndroidShellRenderer : ShellRenderer
{
    public AndroidShellRenderer(Android.Content.Context context) : base(context) { }

    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        => new CustomShellBottomNavViewAppearanceTracker(this, shellItem);
}

public class CustomShellBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
    private readonly IShellContext _shellRenderer;
    private readonly ShellItem _shellItem;
    private bool _subscribedItemReselected;

    public CustomShellBottomNavViewAppearanceTracker(IShellContext shellRenderer, ShellItem shellItem) : base(shellRenderer, shellItem)
    {
        _shellRenderer = shellRenderer;
        _shellItem = shellItem;
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);

        if (_subscribedItemReselected) return;

        bottomView.ItemReselected += (s, e) => ((IShellItemController)_shellItem).ProposeSection(_shellItem.CurrentItem);
        _subscribedItemReselected = true;
    }
}
