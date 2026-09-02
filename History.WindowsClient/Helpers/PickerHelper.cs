using History.WindowsClient.Models;
using Microsoft.Windows.Storage.Pickers;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Helpers;

// File/folder picker helpers mirroring the DialogHelper pattern: UIElement extension
// methods bound to the live XamlRoot's AppWindowId (Windows App SDK picker guidance).
// The pickers return lightweight path-based results (PickFileResult/PickFolderResult);
// convert to StorageFile/StorageFolder via GetFileFromPathAsync when needed.
public static class PickerHelper
{
    public static async Task<PickFileResult> PickFileAsync(this UIElement element, FileOpenPickerParameters parameters)
    {
        var fileOpenPicker = new FileOpenPicker(element.XamlRoot.ContentIslandEnvironment.AppWindowId);
        foreach (var fileTypeFilter in parameters.FileTypeFilters) fileOpenPicker.FileTypeFilter.Add(fileTypeFilter);
        if (parameters.SuggestedStartLocation.HasValue) fileOpenPicker.SuggestedStartLocation = parameters.SuggestedStartLocation.Value;
        if (!string.IsNullOrEmpty(parameters.CommitButtonText)) fileOpenPicker.CommitButtonText = parameters.CommitButtonText;
        return await fileOpenPicker.PickSingleFileAsync();
    }

    public static async Task<IReadOnlyList<PickFileResult>> PickFilesAsync(this UIElement element, FileOpenPickerParameters parameters)
    {
        var fileOpenPicker = new FileOpenPicker(element.XamlRoot.ContentIslandEnvironment.AppWindowId);
        foreach (var fileTypeFilter in parameters.FileTypeFilters) fileOpenPicker.FileTypeFilter.Add(fileTypeFilter);
        if (parameters.SuggestedStartLocation.HasValue) fileOpenPicker.SuggestedStartLocation = parameters.SuggestedStartLocation.Value;
        if (!string.IsNullOrEmpty(parameters.CommitButtonText)) fileOpenPicker.CommitButtonText = parameters.CommitButtonText;
        return await fileOpenPicker.PickMultipleFilesAsync();
    }

    public static async Task<PickFileResult> SaveFileAsync(this UIElement element, FileSavePickerParameters parameters)
    {
        var fileSavePicker = new FileSavePicker(element.XamlRoot.ContentIslandEnvironment.AppWindowId);
        foreach (var(label, extensions)in parameters.FileTypeChoices) fileSavePicker.FileTypeChoices.Add(label, [..extensions]);
        if (!string.IsNullOrEmpty(parameters.SuggestedFileName)) fileSavePicker.SuggestedFileName = parameters.SuggestedFileName;
        if (!string.IsNullOrEmpty(parameters.DefaultFileExtension)) fileSavePicker.DefaultFileExtension = parameters.DefaultFileExtension;
        if (parameters.SuggestedStartLocation.HasValue) fileSavePicker.SuggestedStartLocation = parameters.SuggestedStartLocation.Value;
        return await fileSavePicker.PickSaveFileAsync();
    }

    public static async Task<PickFolderResult> PickFolderAsync(this UIElement element, FolderPickerParameters parameters)
    {
        var folderPicker = new FolderPicker(element.XamlRoot.ContentIslandEnvironment.AppWindowId);
        if (parameters.SuggestedStartLocation.HasValue) folderPicker.SuggestedStartLocation = parameters.SuggestedStartLocation.Value;
        if (!string.IsNullOrEmpty(parameters.CommitButtonText)) folderPicker.CommitButtonText = parameters.CommitButtonText;
        return await folderPicker.PickSingleFolderAsync();
    }
}