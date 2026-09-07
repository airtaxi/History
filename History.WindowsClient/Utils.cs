using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace History.WindowsClient;

public static class Utils
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
}