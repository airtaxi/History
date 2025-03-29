using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<FeedReactionType>))]
public enum FeedReactionType
{
    Like,
    Love,
    Happy,
    Sad,
    Support
}
