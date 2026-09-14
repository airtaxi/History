using System.Collections.Concurrent;
using System.Net.Http;
using History.Commons;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace History.WindowsClient.Helpers;

// Kakao Story emoticon CDN URLs are hotlink-protected with allow_referer=story.kakao.com,
// so the request must carry the Kakao Story Referer. WinUI's BitmapImage(Uri) cannot attach
// a custom Referer header, so emoticon bytes are downloaded here with the required header and
// decoded into a BitmapImage through an in-memory stream. Responses are cached in memory
// because a failed load would otherwise re-request the same image on every re-realization.
public static class KakaoEmoticonImageLoader
{
    private const string EmoticonUrlPrefix = "https://mk.kakaocdn.net/dna/emoticons";
    private const string KakaoStoryReferer = "https://story.kakao.com/";
    private const int CacheEntryLimit = 300;

    private static readonly HttpClient s_httpClient = new();
    private static readonly ConcurrentDictionary<string, byte[]> s_cache = new();

    // Loads an image source for the given media id. A Kakao Story emoticon URL is fetched
    // with the Referer header first; every other media id loads through BitmapImage directly.
    // Returns null when the emoticon fetch fails.
    public static async Task<BitmapImage> CreateImageSourceAsync(string mediaId)
    {
        var url = CommonUtils.GenerateMediaUri(mediaId);
        if (url == null) return null;

        if (!url.StartsWith(EmoticonUrlPrefix, StringComparison.Ordinal)) return new BitmapImage(new Uri(url));

        var imageBytes = await GetEmoticonBytesAsync(url);
        if (imageBytes == null) return null;

        try { return await CreateBitmapImageAsync(imageBytes); }
        catch { return null; }
    }

    // Downloads the emoticon with the Referer header Kakao Story requires, caching the bytes
    // so scrolled-back content renders without another request. Returns null on failure.
    private static async Task<byte[]> GetEmoticonBytesAsync(string url)
    {
        if (s_cache.TryGetValue(url, out var cachedBytes)) return cachedBytes;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri(KakaoStoryReferer);
            using var response = await s_httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            if (s_cache.Count >= CacheEntryLimit) s_cache.Clear();
            s_cache[url] = imageBytes;
            return imageBytes;
        }
        catch { return null; }
    }

    private static async Task<BitmapImage> CreateBitmapImageAsync(byte[] imageBytes)
    {
        var bitmapImage = new BitmapImage();
        using (var stream = new InMemoryRandomAccessStream())
        {
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                using var dataWriter = new DataWriter(outputStream);
                dataWriter.WriteBytes(imageBytes);
                await dataWriter.StoreAsync();
                await dataWriter.FlushAsync();
            }
            stream.Seek(0);
            await bitmapImage.SetSourceAsync(stream);
        }
        return bitmapImage;
    }
}
