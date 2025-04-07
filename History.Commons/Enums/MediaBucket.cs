using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<MediaBucket>))]
public enum MediaBucket
{
    Profile,
    Background,
    Post,
    Comment,
    Sticker
}