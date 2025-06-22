using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.MobileClient;

public static class DialogHelper
{
    private static ContentDialog s_lastContentDialog;
    private static SemaphoreSlim s_showMessageDialogSemaphore = new(1, 1);

    public static ContentDialog GenerateMessageDialog(this UIElement element, string title, string content, string primaryButtonText = Constants.PromptOk, string secondaryButtonText = null)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText
        };
        if (secondaryButtonText != null) dialog.SecondaryButtonText = secondaryButtonText;
        dialog.XamlRoot = element.XamlRoot;
        return dialog;
    }

    public static async Task<ContentDialogResult> ShowMessageDialogAsync(this UIElement element, string title, string content, string primaryButtonText = Constants.PromptOk, string secondaryButtonText = null)
    {
        await s_showMessageDialogSemaphore.WaitAsync();
        try
        {
            var dialog = element.GenerateMessageDialog(title, content, primaryButtonText, secondaryButtonText);
            s_lastContentDialog?.Hide();
            var result = await dialog.ShowAsync();
            s_lastContentDialog = dialog;
            return result;
        }
        finally { s_showMessageDialogSemaphore.Release(); }
    }
}
