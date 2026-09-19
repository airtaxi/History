using System.Text.Json.Serialization;

namespace History.WindowsClient.Services;

// Source-generated JSON context for the media cache index file, keeping index.json serialization
// trim-safe in the trimmed client publish.
[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(Dictionary<string, MediaCacheService.MediaCacheEntry>))]
internal partial class MediaCacheJsonSerializerContext : JsonSerializerContext;
