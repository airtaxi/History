using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public partial class BasePage : Page
{
    protected virtual BaseViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.MessageDialogRequested += OnMessageDialogRequested;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel.MessageDialogRequested -= OnMessageDialogRequested;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = this.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }
}
