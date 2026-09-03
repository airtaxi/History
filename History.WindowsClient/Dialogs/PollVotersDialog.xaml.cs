using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

// Poll voters dialog: loads and lists the voters of a single poll option. The primary
// "목록 보기" button returns to the results dialog (re-runs ViewResultsAsync).
public sealed partial class PollVotersDialog : ContentDialog
{
    public PollVotersViewModel ViewModel { get; }

    public PollVotersDialog(PollVotersViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Opened += OnDialogOpened;
    }

    private async void OnDialogOpened(object sender, ContentDialogOpenedEventArgs args) => await ViewModel.InitializeAsync();
}