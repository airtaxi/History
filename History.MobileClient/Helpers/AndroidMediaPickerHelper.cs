#if ANDROID
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.DocumentFile.Provider;
using History.MobileClient.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Android.App.Application;

namespace History.MobileClient.Helpers;

public static class AndroidMediaPickerHelper
{
    private static TaskCompletionSource<List<MediaFile>> _completionSource = null!;
    private static int _maxSelection = 1;
    private static bool _allowMultiple = false;
    private static ActivityResultLauncher _launcher = null!;

    /// <summary>
    /// Registers the ActivityResultLauncher used to pick media. Must be called
    /// from the activity's OnCreate before the activity is resumed.
    /// </summary>
    public static void Initialize(ActivityResultLauncher launcher) => _launcher = launcher;

    /// <summary>
    /// Bridges the ActivityResultLauncher callback (the Java binding is non-generic)
    /// into the helper, unwrapping the AndroidX <see cref="ActivityResult"/> payload.
    /// </summary>
    public sealed class MediaPickActivityResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object result)
        {
            var activityResult = result as ActivityResult;
            var resultCode = activityResult?.ResultCode;
            AndroidMediaPickerHelper.OnActivityResult(resultCode.HasValue ? (Result)resultCode.Value : Result.Canceled, activityResult?.Data);
        }
    }

    public static Task<List<MediaFile>> PickMediasAsync(int maxCount, bool includeImage, bool includeVideo)
    {
        _maxSelection = maxCount;
        _allowMultiple = true;

        return LaunchPickerAsync(includeImage, includeVideo);
    }

    public static async Task<MediaFile> PickMediaAsync(bool includeImage, bool includeVideo)
    {
        _maxSelection = 1;
        _allowMultiple = false;

        var result = await LaunchPickerAsync(includeImage, includeVideo);
        return result.FirstOrDefault();
    }

    private static Task<List<MediaFile>> LaunchPickerAsync(bool includeImage, bool includeVideo)
    {
        if (!includeImage && !includeVideo) throw new ArgumentException("At least one of includeImage or includeVideo must be true.");

        var intent = new Intent(Intent.ActionPick);
        if (includeImage && includeVideo)
        {
            var mimeTypes = new List<string>();
            if (includeImage) mimeTypes.Add("image/*");
            if (includeVideo) mimeTypes.Add("video/*");

            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes.ToArray());
        }
        else if (includeImage) intent.SetType("image/*");
        else if (includeVideo) intent.SetType("video/*");
        if (_allowMultiple) intent.PutExtra(Intent.ExtraAllowMultiple, _allowMultiple);

        _completionSource = new TaskCompletionSource<List<MediaFile>>();

        _launcher.Launch(intent);

        return _completionSource.Task;
    }

    public static void OnActivityResult(Result resultCode, Intent data)
    {
        var uris = new List<Android.Net.Uri>();
        if (resultCode == Result.Ok && data != null)
        {
            if (data.ClipData != null)
            {
                for (int i = 0; i < data.ClipData.ItemCount && uris.Count < _maxSelection; i++)
                    uris.Add(data.ClipData.GetItemAt(i).Uri);
            }
            else if (data.Data != null) uris.Add(data.Data);
        }

        _ = SetResultAsync(uris);
    }

    private static async Task SetResultAsync(List<Android.Net.Uri> uris)
    {
        try
        {
            // Load the media off the UI thread. Multiple selections are copied to
            // unique temp files in parallel so large photos never block the UI.
            var medias = _allowMultiple
                ? await Task.Run(() => LoadMediaFiles(uris))
                : await Task.Run(() => uris.Select(GetMediaFile).ToList());
            _completionSource.SetResult(medias);
        }
        catch { _completionSource.SetResult([]); }
    }

    /// <summary>
    /// Copies each URI stream into a unique temp file in parallel. The order of
    /// the returned list matches the order of the input URIs.
    /// </summary>
    public static List<MediaFile> LoadMediaFiles(List<Android.Net.Uri> uris)
    {
        var medias = new MediaFile[uris.Count];
        Parallel.For(0, uris.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            medias[i] = LoadMediaFileToTemp(uris[i]));
        return [.. medias];
    }

    private static MediaFile LoadMediaFileToTemp(Android.Net.Uri uri)
    {
        var context = Application.Context;
        var resolver = context.ContentResolver;
        var documentFile = DocumentFile.FromSingleUri(context, uri);

        if (documentFile == null || !documentFile.Exists()) throw new InvalidOperationException("Unable to resolve selected media file.");

        var name = documentFile.Name;
        if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Unable to determine file name.");

        var extension = Path.GetExtension(name);
        var tempFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
        var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

        long size;
        using (var stream = resolver.OpenInputStream(uri) ?? throw new InvalidOperationException("Unable to open input stream."))
        using (var fileStream = File.Create(tempPath))
        {
            stream.CopyTo(fileStream);
            size = fileStream.Length;
        }

        return new MediaFile(name, null) { FilePath = tempPath, Size = size };
    }

    public static MediaFile GetMediaFile(Android.Net.Uri uri)
    {
        var context = Application.Context;
        var resolver = context.ContentResolver;
        var documentFile = DocumentFile.FromSingleUri(context, uri);

        if (documentFile == null || !documentFile.Exists()) throw new InvalidOperationException("Unable to resolve selected media file.");

        var name = documentFile.Name;
        if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Unable to determine file name.");

        using var stream = resolver.OpenInputStream(uri) ?? throw new InvalidOperationException("Unable to open input stream.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return new MediaFile(name, memory.ToArray());
    }
}
#endif