using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}