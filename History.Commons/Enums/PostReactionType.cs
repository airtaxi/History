using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PostReactionType>))]
public enum PostReactionType
{
    Like,
    Awesome,
    Happy,
    Sad,
    Support
}
