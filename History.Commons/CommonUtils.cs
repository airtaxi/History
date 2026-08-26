using System;
using System.Collections.Generic;
using System.Text;

namespace History.Commons;

public static class CommonUtils
{
    private static readonly HttpClient s_httpClient = new();
    private static readonly SemaphoreSlim s_stickerImageDataCacheSemaphore = new(1, 1);
    private static readonly Dictionary<string, byte[]> s_stickerImageDataCache = [];

    public static async Task<byte[]> GetStickerImageDataAsync(string stickerMediaId)
    {
        if (string.IsNullOrEmpty(stickerMediaId)) return [];

        await s_stickerImageDataCacheSemaphore.WaitAsync();
        try
        {
            if (s_stickerImageDataCache.TryGetValue(stickerMediaId, out var cachedImageData)) return cachedImageData;
        }
        finally { s_stickerImageDataCacheSemaphore.Release(); }

        try
        {
            var mediaUri = GenerateMediaUri(stickerMediaId);
            if (mediaUri == null) return [];

            var imageData = await s_httpClient.GetByteArrayAsync(mediaUri);

            await s_stickerImageDataCacheSemaphore.WaitAsync();
            try { s_stickerImageDataCache[stickerMediaId] = imageData; }
            finally { s_stickerImageDataCacheSemaphore.Release(); }

            return imageData;
        }
        catch
        {
            return [];
        }
    }

    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }
}
