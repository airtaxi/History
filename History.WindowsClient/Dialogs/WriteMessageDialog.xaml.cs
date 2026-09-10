using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

public sealed partial class WriteMessageDialog : ContentDialog
{
    public HistoryWriteMessageDialogViewModel ViewModel { get; }

    public WriteMessageDialog(HistoryWriteMessageDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void OnReceiverTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.FilterFriends(sender.Text);
        }
    }

    private void OnReceiverSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is BaseFriendshipViewModel friendshipViewModel)
        {
            ViewModel.SelectReceiver(friendshipViewModel);
            sender.Text = string.Empty;
        }
    }

    private void OnClearReceiverButtonClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearReceiver();
        ReceiverAutoSuggestBox.Text = string.Empty;
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
