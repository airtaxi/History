#if ANDROID
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using History.MobileClient.DataTypes;
using Microsoft.Maui.ApplicationModel;
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

    private const int MediaRequestCode = 39001;

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
        

        //var intent = new Intent(Intent.ActionOpenDocument);
        //intent.AddCategory(Intent.CategoryOpenable);
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

        var activity = Platform.CurrentActivity ?? Application.Context as Activity;
        _completionSource = new TaskCompletionSource<List<MediaFile>>();

        activity!.StartActivityForResult(intent, MediaRequestCode);

        PlatformActivityResultHandler.ActivityResultReceived -= OnActivityResult;
        PlatformActivityResultHandler.ActivityResultReceived += OnActivityResult;

        return _completionSource.Task;
    }

    private static void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (requestCode != MediaRequestCode) return;

        PlatformActivityResultHandler.ActivityResultReceived -= OnActivityResult;

        var medias = new List<MediaFile>();
        if (resultCode == Result.Ok && data != null)
        {
            if (data.ClipData != null)
            {
                for (int i = 0; i < data.ClipData.ItemCount && medias.Count < _maxSelection; i++)
                {
                    var uri = data.ClipData.GetItemAt(i).Uri;
                    var media = GetMediaFile(uri);
                    medias.Add(media);
                }
            }
            else if (data.Data != null) medias.Add(GetMediaFile(data.Data));
        }

        _completionSource.SetResult(medias);
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
