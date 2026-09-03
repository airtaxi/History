using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

// Poll results dialog: renders the poll's current results (percentages, vote counts)
public sealed partial class PollResultsDialog : ContentDialog
{
    public PollContentViewModel ViewModel { get; }

    public PollResultsDialog(PollContentViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}