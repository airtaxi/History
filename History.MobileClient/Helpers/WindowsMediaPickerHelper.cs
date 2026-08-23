#if WINDOWS
using History.MobileClient.DataTypes;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace History.MobileClient.Helpers;

public static class WindowsMediaPickerHelper
{
    private static readonly List<string> s_imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".bmp", ".tif", ".tiff"];
    private static readonly List<string> s_videoExtensions = [".mp4", ".mov", ".avi", ".mkv", ".webm"];

    public static async Task<MediaFile> PickMediaAsync(bool includeImage, bool includeVideo)
    {
        var files = await PickFilesAsync(1, includeImage, includeVideo, true);
        return files.FirstOrDefault();
    }

    public static async Task<List<MediaFile>> PickMediasAsync(int maxCount, bool includeImage, bool includeVideo)
    {
        var files = await PickFilesAsync(maxCount, includeImage, includeVideo, false);
        return [.. files.Take(maxCount)];
    }

    /// <summary>
    /// Saves the media at the given file path through the system file save picker.
    /// </summary>
    public static async Task SaveMediaAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(fileName);

        var fileSavePicker = new FileSavePicker(GetAppWindowId());
        fileSavePicker.FileTypeChoices.Add($"{extension.TrimStart('.').ToUpperInvariant()}", [extension]);
        fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        fileSavePicker.SuggestedFileName = fileName;
        fileSavePicker.DefaultFileExtension = extension;

        var result = await fileSavePicker.PickSaveFileAsync();
        if (result == null) return;

        File.Copy(filePath, result.Path);
    }

    private static async Task<List<MediaFile>> PickFilesAsync(int maxCount, bool includeImage, bool includeVideo, bool loadToMemory)
    {
        if (!includeImage && !includeVideo) throw new ArgumentException("At least one of includeImage or includeVideo must be true.");

        var fileOpenPicker = new FileOpenPicker(GetAppWindowId());
        AddFileTypeFilters(fileOpenPicker, includeImage, includeVideo);

        var results = await fileOpenPicker.PickMultipleFilesAsync();
        if (results == null || results.Count == 0) return [];

        var mediaFiles = new List<MediaFile>();
        foreach (var result in results.Take(maxCount))
            mediaFiles.Add(LoadMediaFile(result.Path, loadToMemory));
        return mediaFiles;
    }

    private static MediaFile LoadMediaFile(string path, bool loadToMemory)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(fileName);

        if (loadToMemory) return new MediaFile(fileName, File.ReadAllBytes(path));

        var tempFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
        var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
        var size = new FileInfo(path).Length;

        using (var source = File.OpenRead(path))
        using (var target = File.Create(tempPath))
            source.CopyTo(target);

        return new MediaFile(fileName, null) { FilePath = tempPath, Size = size };
    }

    private static void AddFileTypeFilters(FileOpenPicker fileOpenPicker, bool includeImage, bool includeVideo)
    {
        var extensions = new List<string>();
        if (includeImage) extensions.AddRange(s_imageExtensions);
        if (includeVideo) extensions.AddRange(s_videoExtensions);

        foreach (var extension in extensions)
            if (!fileOpenPicker.FileTypeFilter.Contains(extension)) fileOpenPicker.FileTypeFilter.Add(extension);
    }

    private static WindowId GetAppWindowId()
    {
        var platformWindow = App.MainWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        var xamlRoot = platformWindow?.Content?.XamlRoot;
        if (xamlRoot == null) throw new InvalidOperationException("Unable to resolve the current XamlRoot for the picker.");

        return xamlRoot.ContentIslandEnvironment.AppWindowId;
    }
}
#endif
