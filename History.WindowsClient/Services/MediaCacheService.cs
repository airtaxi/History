using System.Diagnostics;
using System.Net;
using System.Text.Json;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace History.WindowsClient.Services;

// Disk-backed LRU image cache for carousel media. Entries are downloaded on first request,
// stored under LocalCacheFolder/MediaCache with a 1 GB cap, and their decoded pixel dimensions
// are recorded in index.json so carousels can compute their initial height before the bitmap
// decodes. The least recently used entries are evicted when the cap is exceeded. Raw video
// files are never cached; video posts display their thumbnail image, which is cached normally.
public static class MediaCacheService
{
    private const string CacheFolderName = "MediaCache";
    private const string CacheIndexFileName = "index.json";
    private const long MaxCacheSizeBytes = 1024L * 1024 * 1024; // 1 GB
    private const int MaxConcurrentPrefetchDownloads = 4;
    private const int MaxRateLimitRetries = 10;
    private static readonly TimeSpan s_rateLimitRetryMinDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan s_rateLimitRetryMaxDelay = TimeSpan.FromMilliseconds(1000);

    // Serialized cache entry kept in index.json. LastAccessTimeUtc drives LRU eviction.
    public sealed class MediaCacheEntry
    {
        public string FileName { get; set; }
        public int PixelWidth { get; set; }
        public int PixelHeight { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
    }

    private static readonly HttpClient s_httpClient = new();
    private static readonly SemaphoreSlim s_indexSemaphore = new(1, 1);
    private static readonly SemaphoreSlim s_inFlightSemaphore = new(1, 1);
    private static readonly Dictionary<string, Task> s_inFlightDownloads = [];
    private static bool s_isIndexLoaded;
    private static Dictionary<string, MediaCacheEntry> s_cacheIndex = [];
    private static long s_totalCacheSizeBytes;

    // Reads the cached pixel dimensions without downloading the media, so view models can
    // compute the carousel height immediately. Returns (0, 0) when the media is not cached.
    public static async Task<(int PixelWidth, int PixelHeight)> TryGetPixelSizeAsync(string mediaId)
    {
        if (string.IsNullOrEmpty(mediaId)) return (0, 0);

        await s_indexSemaphore.WaitAsync();
        try
        {
            await EnsureIndexLoadedAsync();
            if (!s_cacheIndex.TryGetValue(mediaId, out var entry)) return (0, 0);

            entry.LastAccessTimeUtc = DateTime.UtcNow; // LRU touch; the index save is deferred to structural changes.
            return (entry.PixelWidth, entry.PixelHeight);
        }
        finally { s_indexSemaphore.Release(); }
    }

    public static async Task<bool> IsCachedAsync(string mediaId) => (await TryGetPixelSizeAsync(mediaId)).PixelWidth > 0;

    // Loads a BitmapImage from the cached file. Returns null when the media is not cached or
    // the file cannot be read, so the caller can keep its network-backed image source.
    public static async Task<BitmapImage> CreateCachedImageSourceAsync(string mediaId)
    {
        try
        {
            await s_indexSemaphore.WaitAsync();
            StorageFile cacheFile;
            try
            {
                await EnsureIndexLoadedAsync();
                if (!s_cacheIndex.TryGetValue(mediaId, out var entry)) return null;

                var cacheFolder = await GetCacheFolderAsync();
                cacheFile = await cacheFolder.TryGetItemAsync(entry.FileName) as StorageFile;
            }
            finally { s_indexSemaphore.Release(); }

            if (cacheFile == null) return null;

            var bitmapImage = new BitmapImage();
            using var fileStream = await cacheFile.OpenReadAsync();
            await bitmapImage.SetSourceAsync(fileStream);
            return bitmapImage;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[MediaCacheService] Failed to load cached image '{mediaId}': {exception.Message}");
            return null;
        }
    }

    // Downloads the media and records its dimensions when not cached yet. Concurrent calls for
    // the same media share one in-flight task. Never throws; failures are logged and swallowed.
    // Rate-limited responses (429) are retried after a randomized 300-1000 ms delay.
    public static async Task DownloadAsync(string mediaId)
    {
        if (string.IsNullOrEmpty(mediaId)) return;
        if (await IsCachedAsync(mediaId)) return;

        Task downloadTask;
        await s_inFlightSemaphore.WaitAsync();
        try
        {
            if (!s_inFlightDownloads.TryGetValue(mediaId, out downloadTask))
            {
                downloadTask = DownloadAndCacheAsync(mediaId);
                s_inFlightDownloads[mediaId] = downloadTask;
            }
        }
        finally { s_inFlightSemaphore.Release(); }

        await downloadTask;
    }

    // Warm-start helper for the timeline load: downloads every carousel image of the given posts
    // before their post view models are created, so each carousel view model is guaranteed a
    // cache hit and its height is final on the first measure pass. Download failures are
    // swallowed (see DownloadAsync); failed images fall back to the network image path.
    public static async Task PrefetchTimelineMediaAsync(IEnumerable<PostResponseDto> posts)
    {
        if (posts == null) return;

        var mediaIds = new List<string>();
        foreach (var post in posts)
        {
            CollectCarouselMediaIds(post, mediaIds);
            if (post.ParentPost != null) CollectCarouselMediaIds(post.ParentPost, mediaIds);
        }

        var distinctMediaIds = mediaIds.Distinct().ToList();
        if (distinctMediaIds.Count == 0) return;

        // Bound the concurrency so pre-fetching a whole page does not open dozens of connections.
        using var downloadThrottle = new SemaphoreSlim(MaxConcurrentPrefetchDownloads);
        await Task.WhenAll(distinctMediaIds.Select(async mediaId =>
        {
            await downloadThrottle.WaitAsync();
            try { await DownloadAsync(mediaId); }
            finally { downloadThrottle.Release(); }
        }));
    }

    // Frees the entire cache (folder and index). Meant for a future cache settings entry point.
    public static async Task ClearAsync()
    {
        await s_indexSemaphore.WaitAsync();
        try
        {
            var cacheFolder = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(CacheFolderName);
            if (cacheFolder != null) await cacheFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);

            s_cacheIndex = [];
            s_totalCacheSizeBytes = 0;
            s_isIndexLoaded = true;
        }
        finally { s_indexSemaphore.Release(); }
    }

    private static void CollectCarouselMediaIds(PostResponseDto post, List<string> mediaIds)
    {
        if (post?.Contents == null) return;

        foreach (var content in post.Contents)
        {
            if (content is not MediaContent mediaContent) continue;

            // Timeline carousels always display the thumbnail (falling back to the original media),
            // mirroring CreateInlineImageSource for the wrapped (non-Unwrapped) post types.
            if (mediaContent.IsVideo && mediaContent.ThumbnailMediaId == null) continue; // Never cache the raw video file.
            if (!string.IsNullOrEmpty(mediaContent.ThumbnailMediaId ?? mediaContent.MediaId)) mediaIds.Add(mediaContent.ThumbnailMediaId ?? mediaContent.MediaId);
        }
    }

    private static async Task DownloadAndCacheAsync(string mediaId)
    {
        try
        {
            var mediaUri = History.Commons.CommonUtils.GenerateMediaUri(mediaId);
            if (mediaUri == null) return;

            byte[] imageBytes = null;
            for (var attempt = 0; attempt <= MaxRateLimitRetries; attempt++)
            {
                try
                {
                    imageBytes = await s_httpClient.GetByteArrayAsync(mediaUri);
                    break;
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRateLimitRetries)
                {
                    // Randomized 300-1000 ms (inclusive) back-off before the next attempt.
                    var retryDelayMilliseconds = Random.Shared.Next((int)s_rateLimitRetryMinDelay.TotalMilliseconds, (int)s_rateLimitRetryMaxDelay.TotalMilliseconds + 1);
                    Debug.WriteLine($"[MediaCacheService] Rate limited on '{mediaId}', retrying in {retryDelayMilliseconds} ms (attempt {attempt + 1}/{MaxRateLimitRetries}).");
                    await Task.Delay(retryDelayMilliseconds);
                }
            }

            if (imageBytes == null)
            {
                Debug.WriteLine($"[MediaCacheService] Gave up downloading '{mediaId}' after {MaxRateLimitRetries + 1} rate-limited attempts.");
                return;
            }

            var cacheFolder = await GetCacheFolderAsync();
            var tempFile = await cacheFolder.CreateFileAsync($"{mediaId}.tmp", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(tempFile, imageBytes);

            var (pixelWidth, pixelHeight) = await GetImagePixelSizeAsync(tempFile);
            if (pixelWidth == 0 || pixelHeight == 0)
            {
                await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                Debug.WriteLine($"[MediaCacheService] Skipped non-image media '{mediaId}'.");
                return;
            }

            await tempFile.RenameAsync($"{mediaId}.bin", NameCollisionOption.ReplaceExisting);

            await s_indexSemaphore.WaitAsync();
            try
            {
                await EnsureIndexLoadedAsync();
                if (s_cacheIndex.TryGetValue(mediaId, out var oldEntry)) s_totalCacheSizeBytes -= oldEntry.FileSizeBytes;
                s_cacheIndex[mediaId] = new MediaCacheEntry { FileName = $"{mediaId}.bin", PixelWidth = pixelWidth, PixelHeight = pixelHeight, FileSizeBytes = imageBytes.LongLength, LastAccessTimeUtc = DateTime.UtcNow };
                s_totalCacheSizeBytes += imageBytes.LongLength;
                await SaveIndexAsync();
                await EnforceCacheLimitAsync();
            }
            finally { s_indexSemaphore.Release(); }
        }
        catch (Exception exception) { Debug.WriteLine($"[MediaCacheService] Failed to download '{mediaId}': {exception.Message}"); }
        finally
        {
            await s_inFlightSemaphore.WaitAsync();
            try { s_inFlightDownloads.Remove(mediaId); }
            finally { s_inFlightSemaphore.Release(); }
        }
    }

    private static async Task EnsureIndexLoadedAsync()
    {
        if (s_isIndexLoaded) return;

        s_cacheIndex = [];
        s_totalCacheSizeBytes = 0;

        try
        {
            var cacheFolder = await GetCacheFolderAsync();
            var indexFile = await cacheFolder.TryGetItemAsync(CacheIndexFileName) as StorageFile;
            if (indexFile != null)
            {
                var indexJson = await FileIO.ReadTextAsync(indexFile);
                var loadedIndex = JsonSerializer.Deserialize<Dictionary<string, MediaCacheEntry>>(indexJson);
                if (loadedIndex != null)
                {
                    s_cacheIndex = loadedIndex;
                    foreach (var entry in s_cacheIndex.Values) s_totalCacheSizeBytes += entry.FileSizeBytes;
                }
            }
        }
        catch (Exception exception) { Debug.WriteLine($"[MediaCacheService] Failed to load the cache index: {exception.Message}"); }

        s_isIndexLoaded = true;
    }

    private static async Task SaveIndexAsync()
    {
        if (!s_isIndexLoaded) return;

        var cacheFolder = await GetCacheFolderAsync();
        var indexFile = await cacheFolder.CreateFileAsync(CacheIndexFileName, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(indexFile, JsonSerializer.Serialize(s_cacheIndex));
    }

    // Deletes the oldest entries (and their files) until the total size fits within
    // MaxCacheSizeBytes. The caller must already hold s_indexSemaphore.
    private static async Task EnforceCacheLimitAsync()
    {
        if (s_totalCacheSizeBytes <= MaxCacheSizeBytes) return;

        var cacheFolder = await GetCacheFolderAsync();
        var entriesByOldest = s_cacheIndex.OrderBy(x => x.Value.LastAccessTimeUtc).ToList();

        foreach (var (mediaId, entry) in entriesByOldest)
        {
            try
            {
                var cacheFile = await cacheFolder.TryGetItemAsync(entry.FileName);
                if (cacheFile != null) await cacheFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception exception) { Debug.WriteLine($"[MediaCacheService] Failed to evict '{entry.FileName}': {exception.Message}"); }

            s_cacheIndex.Remove(mediaId);
            s_totalCacheSizeBytes -= entry.FileSizeBytes;

            if (s_totalCacheSizeBytes <= MaxCacheSizeBytes) break;
        }

        await SaveIndexAsync();
    }

    private static async Task<(int PixelWidth, int PixelHeight)> GetImagePixelSizeAsync(StorageFile imageFile)
    {
        try
        {
            using var fileStream = await imageFile.OpenReadAsync();
            var bitmapDecoder = await BitmapDecoder.CreateAsync(fileStream);
            return ((int)bitmapDecoder.PixelWidth, (int)bitmapDecoder.PixelHeight);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[MediaCacheService] Failed to decode '{imageFile.Name}': {exception.Message}");
            return (0, 0);
        }
    }

    private static async Task<StorageFolder> GetCacheFolderAsync() => await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
}