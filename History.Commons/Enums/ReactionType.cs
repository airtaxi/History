using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ReactionType>))]
public enum ReactionType
{
    Like,
    Awesome,
    Happy,
    Sad,
    Support
}


public static class ReactionTypeExtensions
{
    public static string ToDisplayString(this ReactionType type)
    {
        return type switch
        {
            ReactionType.Like => "좋아요",
            ReactionType.Awesome => "멋져요",
            ReactionType.Happy => "기뻐요",
            ReactionType.Sad => "슬퍼요",
            ReactionType.Support => "힘내요",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static ReactionType FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "좋아요" => ReactionType.Like,
            "멋져요" => ReactionType.Awesome,
            "기뻐요" => ReactionType.Happy,
            "슬퍼요" => ReactionType.Sad,
            "힘내요" => ReactionType.Support,
            _ => throw new ArgumentOutOfRangeException(nameof(displayString), displayString, null)
        };
    }
}