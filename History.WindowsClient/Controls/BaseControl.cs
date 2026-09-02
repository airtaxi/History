using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Controls;

public partial class BaseControl : UserControl
{
    public virtual BaseViewModel ViewModel { get; }

    // XAML-declared lifecycle hooks mirroring BasePage.OnNavigatedTo/OnNavigatedFrom:
    // derived XAML roots declare Loaded="OnControlLoaded" Unloaded="OnControlUnloaded".
    // The wiring persists for the control's lifetime, so Loaded/Unloaded fire on every
    // attach/detach, including recycled instances under virtualization.
    protected virtual void OnControlLoaded(object sender, RoutedEventArgs e) => SubscribeViewModelEvents();

    protected virtual void OnControlUnloaded(object sender, RoutedEventArgs e) => UnsubscribeViewModelEvents();

    private void SubscribeViewModelEvents()
    {
        ViewModel.MessageDialogRequested += OnMessageDialogRequested;
        ViewModel.InputDialogRequested += OnInputDialogRequested;
        ViewModel.ContentDialogRequested += OnContentDialogRequested;
        ViewModel.FilePickRequested += OnFilePickRequested;
        ViewModel.FilesPickRequested += OnFilesPickRequested;
        ViewModel.SaveFileRequested += OnSaveFileRequested;
        ViewModel.FolderPickRequested += OnFolderPickRequested;
        ViewModel.LoadingStateRequested += OnLoadingStateRequested;
        ViewModel.ShowLoadingRequested += OnShowLoadingRequested;
        ViewModel.HideLoadingRequested += OnHideLoadingRequested;
    }

    private void UnsubscribeViewModelEvents()
    {
        ViewModel.MessageDialogRequested -= OnMessageDialogRequested;
        ViewModel.InputDialogRequested -= OnInputDialogRequested;
        ViewModel.ContentDialogRequested -= OnContentDialogRequested;
        ViewModel.FilePickRequested -= OnFilePickRequested;
        ViewModel.FilesPickRequested -= OnFilesPickRequested;
        ViewModel.SaveFileRequested -= OnSaveFileRequested;
        ViewModel.FolderPickRequested -= OnFolderPickRequested;
        ViewModel.LoadingStateRequested -= OnLoadingStateRequested;
        ViewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        ViewModel.HideLoadingRequested -= OnHideLoadingRequested;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = this.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's input dialog requests with the control-bound dialog.
    private void OnInputDialogRequested(object sender, InputDialogRequestedEventArgs args)
    {
        var result = this.ShowInputDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's prebuilt dialog requests with the control-bound dialog.
    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = this.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    // Fulfills the view model's picker requests with the control-bound pickers.
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

    // Forwards the view model's loading requests to the owning window through the
    // weak-reference messenger (the window matches the control's XamlRoot).
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(XamlRoot, args);

    private void OnShowLoadingRequested(object sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(args);

    private void OnHideLoadingRequested(object sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send();
}