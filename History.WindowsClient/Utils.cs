using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace History.WindowsClient;

public static partial class Utils
{
    // Adds a clickable item that runs the given async action when tapped.
    public static MenuFlyoutItem CreateActionItem(string text, string glyph, Func<Task> action, Windows.UI.Color? iconColor = null)
    {
        var item = new MenuFlyoutItem { Text = text, Tag = action };
        if (iconColor != null) item.Icon = new FontIcon { Glyph = glyph, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor.Value) };
        else item.Icon = new FontIcon { Glyph = glyph };
        item.Click += async (sender, _) => await ((Func<Task>)((MenuFlyoutItem)sender).Tag)();
        return item;
    }

    // Adds a clickable item that runs the given synchronous action when tapped.
    public static MenuFlyoutItem CreateActionItem(string text, string glyph, Action action, Windows.UI.Color? iconColor = null)
    {
        var item = new MenuFlyoutItem { Text = text, Tag = action };
        if (iconColor != null) item.Icon = new FontIcon { Glyph = glyph, Foreground = new SolidColorBrush(iconColor.Value) };
        else item.Icon = new FontIcon { Glyph = glyph };
        item.Click += (sender, _) => ((Action)((MenuFlyoutItem)sender).Tag)();
        return item;
    }

    // Copies PNG-encoded image bytes to the clipboard as a bitmap. The stream is kept
    // alive by the RandomAccessStreamReference while the clipboard holds the data, so
    // the DataWriter is intentionally not disposed: its disposal closes the stream.
    public static async Task CopyPngBytesToClipboardAsync(byte[] pngBytes)
    {
        var stream = new InMemoryRandomAccessStream();
        var dataWriter = new DataWriter(stream);
        dataWriter.WriteBytes(pngBytes);
        await dataWriter.StoreAsync();
        await dataWriter.FlushAsync();

        stream.Seek(0);
        SetClipboardBitmap(stream);
    }

    // Downloads the image at the given URI and re-encodes it as PNG, the format the
    // clipboard bitmap path consumes reliably; returns false when the download or
    // decode fails.
    public static async Task<bool> CopyImageFromUriToClipboardAsync(string imageUri)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUri);

            using var inputStream = new InMemoryRandomAccessStream();
            var dataWriter = new DataWriter(inputStream);
            dataWriter.WriteBytes(imageBytes);
            await dataWriter.StoreAsync();
            await dataWriter.FlushAsync();

            inputStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(inputStream);

            var pngStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, pngStream);
            var pixelData = await decoder.GetPixelDataAsync();
            encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.PixelWidth, decoder.PixelHeight, decoder.DpiX, decoder.DpiY, pixelData.DetachPixelData());
            await encoder.FlushAsync();

            pngStream.Seek(0);
            SetClipboardBitmap(pngStream);
            return true;
        }
        catch { return false; }
    }

    // Places the given PNG stream on the clipboard as a bitmap; the stream reference
    // keeps the stream alive while the clipboard holds the data.
    private static void SetClipboardBitmap(InMemoryRandomAccessStream pngStream)
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(pngStream));
        Clipboard.SetContent(dataPackage);
    }

    // Opens a link. History post URLs (https://historyweb.cc/post/{postId}) navigate to
    // the in-app post page; the post id is extracted directly from the URL path.
    // History user profile URLs (https://historyweb.cc/u/{userId}) navigate to the
    // in-app user profile page.
    // Kakao Story post URLs (https://story.kakao.com/{userId}/{postCode}) navigate to
    // the in-app post page; the post id is resolved by fetching the page and extracting
    // the embedded feed_id.
    // Kakao Story user profile URLs (https://story.kakao.com/{userId}) navigate to the
    // in-app user profile page; the user id is extracted directly from the URL path.
    // When the URL is not an in-app target, it opens in the external browser.
    public static async Task OpenLinkAsync(string url)
    {
        var historyUserId = GetHistoryUserId(url);
        if (historyUserId != null)
        {
            NavigateToPage(typeof(ProfilePage), historyUserId);
            return;
        }

        var kakaoStoryUserId = GetKakaoStoryUserId(url);
        if (kakaoStoryUserId != null)
        {
            NavigateToPage(typeof(ProfilePage), new KakaoProfileParameters(kakaoStoryUserId));
            return;
        }

        var historyPostId = GetHistoryPostId(url);
        if (historyPostId != null)
        {
            await OpenHistoryPostAsync(historyPostId);
            return;
        }

        var kakaoStoryPostId = await GetKakaoStoryPostIdAsync(url);
        if (kakaoStoryPostId != null)
        {
            await OpenKakaoStoryPostAsync(kakaoStoryPostId);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        // A story.kakao.com URL that resolved as neither a profile nor a post means the
        // post page fetch or the feed_id extraction failed.
        if (uri.Host == "story.kakao.com")
        {
            await ShowMessageDialogAsync(Constants.ErrorTitle, "카카오스토리 게시글을 불러오지 못했습니다.");
            return;
        }

        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    // Extracts the user id from a historyweb.cc/u/{userId} URL. Returns null when
    // the URL is not a History user profile URL.
    private static string GetHistoryUserId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Host != "historyweb.cc") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || segments[0] != "u") return null;

        return segments[1];
    }

    // Extracts the post id from a historyweb.cc/post/{postId} URL. Returns null when
    // the URL is not a History post URL.
    private static string GetHistoryPostId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Host != "historyweb.cc") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || segments[0] != "post") return null;

        return segments[1];
    }

    // Extracts the user id from a story.kakao.com/{userId} URL. Returns null when
    // the URL is not a Kakao Story user profile URL.
    private static string GetKakaoStoryUserId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Host != "story.kakao.com") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 1) return null;

        return segments[0];
    }

    // Resolves the real post id of a story.kakao.com post URL. The URL only carries
    // a short code (e.g. eNOIUHoOHQA), so the authenticated page must be fetched to
    // read the embedded feed_id (e.g. "_63msr.6MgT2Z7CfP9"). Returns null when the
    // URL is not a story post URL or the id cannot be resolved.
    private static async Task<string> GetKakaoStoryPostIdAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Host != "story.kakao.com") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2) return null;

        if (!await KakaoStoryUtils.EnsureLoggedInAsync()) return null;

        var page = await KakaoStoryApiHandler.GetPostPageAsync(url);
        if (page == null) return null;

        var match = KakaoStoryFeedIdRegex().Match(page);
        if (!match.Success) return null;

        return match.Groups[1].Value;
    }

    // Fetches the History post and navigates to it; the fetched post is passed as the
    // page parameter so the detail page renders without an extra request.
    private static async Task OpenHistoryPostAsync(string postId)
    {
        try
        {
            var post = await CommonShared.ApiHandler.ExecuteRequestAsync<PostResponseDto>(new GetPost(postId));
            NavigateToPage(typeof(PostPage), post);
        }
        catch (Exception exception) { await ShowMessageDialogAsync(Constants.ErrorTitle, $"게시글을 불러오지 못했습니다.\n{exception.Message}"); }
    }

    // Fetches the Kakao Story post and navigates to it; a missing session shows the
    // Kakao Story login flow first.
    private static async Task OpenKakaoStoryPostAsync(string postId)
    {
        try
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync()) return;

            var post = await KakaoStoryApiHandler.GetPost(postId);
            if (post == null)
            {
                await ShowMessageDialogAsync(Constants.ErrorTitle, "카카오스토리 게시글을 불러오지 못했습니다.");
                return;
            }

            NavigateToPage(typeof(PostPage), post);
        }
        catch (Exception exception) { await ShowMessageDialogAsync(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}"); }
    }

    // Focuses the window and navigates its root frame, marshalling to the UI thread
    // because the API continuation can resume on a background thread.
    private static void NavigateToPage(Type pageType, object parameter)
    {
        MainWindow.SetForegroundWindow();
        var frame = MainWindow.Frame;
        if (frame.DispatcherQueue.HasThreadAccess) frame.Navigate(pageType, parameter);
        else frame.DispatcherQueue.TryEnqueue(() => frame.Navigate(pageType, parameter));
    }

    // Shows a message dialog on the window's UI thread so callers on any continuation
    // thread can report link handling failures.
    private static async Task ShowMessageDialogAsync(string title, string message)
    {
        var frame = MainWindow.Frame;
        if (frame.DispatcherQueue.HasThreadAccess)
        {
            await frame.ShowMessageDialogAsync(new MessageDialogParameters(title, message));
            return;
        }

        var taskCompletionSource = new TaskCompletionSource<ContentDialogResult>();
        frame.DispatcherQueue.TryEnqueue(async () => taskCompletionSource.TrySetResult(await frame.ShowMessageDialogAsync(new MessageDialogParameters(title, message))));
        await taskCompletionSource.Task;
    }

    // Captures the feed_id from a story.kakao.com post page response. The feed_id
    // is the real post id used by the story API (e.g. "_63msr.6MgT2Z7CfP9").
    [GeneratedRegex("\"feed_id\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled)]
    private static partial Regex KakaoStoryFeedIdRegex();
}