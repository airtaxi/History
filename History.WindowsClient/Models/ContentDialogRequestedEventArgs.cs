using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Models;

// Dialog counterpart of MessageDialogRequestedEventArgs: carries a prebuilt ContentDialog
// to the host page and receives its dismissal result.
public sealed class ContentDialogRequestedEventArgs(ContentDialog dialog) : EventArgs
{
    public ContentDialog Dialog { get; } = dialog;

    public Task<ContentDialogResult> ResultTask { get; set; }
}