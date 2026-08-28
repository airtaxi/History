using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Resources;

public sealed partial class Comment : ResourceDictionary
{
    public Comment() => InitializeComponent();

    // Fills the comment "..." menu with the actions of the comment view model bound to the button.
    private void OnCommentMenuFlyoutOpening(object sender, object e)
    {
        if (sender is not MenuFlyout menuFlyout) return;
        if (menuFlyout.Target?.Tag is not BaseCommentViewModel commentViewModel) return;

        commentViewModel.PopulateMoreMenuFlyout(menuFlyout);
    }
}