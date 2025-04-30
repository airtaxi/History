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


public static class PostReactionTypeExtensions
{
    public static string ToDisplayString(this PostReactionType type)
    {
        return type switch
        {
            PostReactionType.Like => "좋아요",
            PostReactionType.Awesome => "멋져요",
            PostReactionType.Happy => "기뻐요",
            PostReactionType.Sad => "슬퍼요",
            PostReactionType.Support => "힘내요",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static PostReactionType FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "좋아요" => PostReactionType.Like,
            "멋져요" => PostReactionType.Awesome,
            "기뻐요" => PostReactionType.Happy,
            "슬퍼요" => PostReactionType.Sad,
            "힘내요" => PostReactionType.Support,
            _ => throw new ArgumentOutOfRangeException(nameof(displayString), displayString, null)
        };
    }
}