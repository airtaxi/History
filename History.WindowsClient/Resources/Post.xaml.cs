using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.Resources;

public sealed partial class Post : ResourceDictionary
{
    public Post() => InitializeComponent();

    // Fills the "..." menu with the actions of the post view model bound to the button.
    private void OnMoreMenuFlyoutOpening(object sender, object e)
    {
        if (sender is not MenuFlyout menuFlyout) return;
        if (menuFlyout.Target?.Tag is not BasePostViewModel postViewModel) return;

        postViewModel.PopulateMoreMenuFlyout(menuFlyout);
    }

    // Fills the reaction menu with the five reactions or a cancel entry.
    private void OnReactionMenuFlyoutOpening(object sender, object e)
    {
        if (sender is not MenuFlyout menuFlyout) return;
        if (menuFlyout.Target?.Tag is not BasePostViewModel postViewModel) return;

        postViewModel.PopulateReactionMenuFlyout(menuFlyout);
    }

    // Swallows the pointer press so the timeline card's outer button cannot also raise its own
    // Click (button Click is pointer-driven, so marking Tapped handled alone would not stop the
    // chained navigation to the wrapper post).
    private void OnSharedPostPointerPressed(object sender, PointerRoutedEventArgs e) => e.Handled = true;

    // Mirrors the MAUI SharedPostTemplate: tapping the shared post's original opens the original
    // post itself instead of chaining to the wrapper post's tap.
    private async void OnSharedPostTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (IsInsideInteractiveElement(originalSource, (FrameworkElement)sender)) return;

        e.Handled = true;

        if (sender is FrameworkElement { DataContext: BasePostViewModel viewModel }) await viewModel.HandleTapAsync();
    }

    // Taps that originate inside a button (profile/share/video overlays) keep their own behavior.
    private static bool IsInsideInteractiveElement(DependencyObject source, FrameworkElement root)
    {
        DependencyObject current = source;
        while (current != null && current != root)
        {
            if (current is ButtonBase) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }
}