#if ANDROID
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using History.MobileClient.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Android.App.Application;

namespace History.MobileClient.Helpers;

public static class PlatformActivityResultHandler
{
    public static event Action<int, Result, Intent> ActivityResultReceived;

    public static void OnActivityResult(int requestCode, Result resultCode, Intent data) => ActivityResultReceived?.Invoke(requestCode, resultCode, data);
}

public static class AndroidMediaPickerHelper
{
    private static TaskCompletionSource<List<MediaFile>> _completionSource = null!;
    private static int _maxSelection = 1;
    private static bool _allowMultiple = false;
    private static int _currentRequestCode = -1;

    private const int MediaRequestCode = 39001;

    public static Task<List<MediaFile>> PickMediasAsync(int maxCount, bool includeImage, bool includeVideo)
    {
        _maxSelection = maxCount;
        _allowMultiple = true;

        return LaunchPickerAsync(includeImage, includeVideo, MediaRequestCode);
    }

    public static async Task<MediaFile> PickMediaAsync(bool includeImage, bool includeVideo)
    {
        _maxSelection = 1;
        _allowMultiple = false;

        var results = await PickMediasAsync(1, includeImage, includeVideo);
        if (results.Count == 0)
            throw new InvalidOperationException("No media selected.");

        return results[0];
    }

    private static Task<List<MediaFile>> LaunchPickerAsync(bool includeImage, bool includeVideo, int requestCode)
    {
        if (!includeImage && !includeVideo) throw new ArgumentException("At least one of includeImage or includeVideo must be true.");
        
        var mimeTypes = new List<string>();
        if (includeImage) mimeTypes.Add("image/*");
        if (includeVideo) mimeTypes.Add("video/*");

        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("*/*");
        intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes.ToArray());
        if (_allowMultiple) intent.PutExtra(Intent.ExtraAllowMultiple, _allowMultiple);

        var activity = Platform.CurrentActivity ?? Application.Context as Activity;
        _completionSource = new TaskCompletionSource<List<MediaFile>>();
        _currentRequestCode = requestCode;

        activity!.StartActivityForResult(intent, requestCode);

        PlatformActivityResultHandler.ActivityResultReceived -= OnActivityResult;
        PlatformActivityResultHandler.ActivityResultReceived += OnActivityResult;

        return _completionSource.Task;
    }

    private static void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (requestCode != _currentRequestCode) return;

        PlatformActivityResultHandler.ActivityResultReceived -= OnActivityResult;

        var result = new List<MediaFile>();
        if (resultCode == Result.Ok && data != null)
        {
            var resolver = Application.Context.ContentResolver;

            if (data.ClipData != null)
            {
                for (int i = 0; i < data.ClipData.ItemCount && result.Count < _maxSelection; i++)
                {
                    var uri = data.ClipData.GetItemAt(i).Uri;
                    result.Add(GetMediaFile(uri));
                }
            }
            else if (data.Data != null) result.Add(GetMediaFile(data.Data));
        }

        _completionSource.SetResult(result);
    }

    private static MediaFile GetMediaFile(Android.Net.Uri uri)
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