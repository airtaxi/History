using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Pages;

public partial class BasePage : Page
{
    protected virtual BaseViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.MessageDialogRequested += OnMessageDialogRequested;
        ViewModel.ContentDialogRequested += OnContentDialogRequested;
        ViewModel.FilePickRequested += OnFilePickRequested;
        ViewModel.FilesPickRequested += OnFilesPickRequested;
        ViewModel.SaveFileRequested += OnSaveFileRequested;
        ViewModel.FolderPickRequested += OnFolderPickRequested;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel.MessageDialogRequested -= OnMessageDialogRequested;
        ViewModel.ContentDialogRequested -= OnContentDialogRequested;
        ViewModel.FilePickRequested -= OnFilePickRequested;
        ViewModel.FilesPickRequested -= OnFilesPickRequested;
        ViewModel.SaveFileRequested -= OnSaveFileRequested;
        ViewModel.FolderPickRequested -= OnFolderPickRequested;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = this.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's prebuilt dialog requests with the page-bound dialog.
    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = this.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    // Fulfills the view model's picker requests with the page-bound pickers.
    private void OnFilePickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args)
    {
        var result = this.PickFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFilesPickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args)
    {
        var result = this.PickFilesAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnSaveFileRequested(object sender, PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult> args)
    {
        var result = this.SaveFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFolderPickRequested(object sender, PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult> args)
    {
        var result = this.PickFolderAsync(args.Parameters);
        args.ResultTask = result;
    }
}