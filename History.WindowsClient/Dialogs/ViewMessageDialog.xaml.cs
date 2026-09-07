using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

public sealed partial class ViewMessageDialog : ContentDialog
{
    public BaseMessageViewModel ViewModel { get; }

    public ViewMessageDialog(BaseMessageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
