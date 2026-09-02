using CommunityToolkit.Mvvm.ComponentModel;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    public event EventHandler<MessageDialogRequestedEventArgs> MessageDialogRequested;
    public event EventHandler<ContentDialogRequestedEventArgs> ContentDialogRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>> FilePickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>> FilesPickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>> SaveFileRequested;
    public event EventHandler<PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>> FolderPickRequested;

    public async Task<ContentDialogResult?> ShowMessageDialogAsync(MessageDialogParameters parameters)
    {
        var args = new MessageDialogRequestedEventArgs(parameters);
        MessageDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Prebuilt ContentDialog requests fulfilled by the host page (mirrors ShowMessageDialogAsync).
    public async Task<ContentDialogResult?> ShowContentDialogAsync(ContentDialog dialog)
    {
        var args = new ContentDialogRequestedEventArgs(dialog);
        ContentDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Picker requests fulfilled by the host page (mirrors ShowMessageDialogAsync).
    public async Task<PickFileResult> PickFileAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>(parameters);
        FilePickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<IReadOnlyList<PickFileResult>> PickFilesAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>(parameters);
        FilesPickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFileResult> SaveFileAsync(FileSavePickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>(parameters);
        SaveFileRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFolderResult> PickFolderAsync(FolderPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>(parameters);
        FolderPickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }
}