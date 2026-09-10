using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

public sealed partial class ReplyMessageDialog : ContentDialog
{
    public HistoryReplyMessageDialogViewModel ViewModel { get; }

    public ReplyMessageDialog(HistoryReplyMessageDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var sent = await ViewModel.SendAsync();
            if (!sent) args.Cancel = true;
        }
        finally { deferral.Complete(); }
    }
}
