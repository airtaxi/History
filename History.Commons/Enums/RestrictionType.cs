using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RestrictionType>))]
public enum RestrictionType
{
    PostDeletion,
    CommentDeletion
}

public static class RestrictionTypeExtensions
{
    public static string ToDisplayString(this RestrictionType type)
    {
        return type switch
        {
            RestrictionType.PostDeletion => "게시물 삭제",
            RestrictionType.CommentDeletion => "댓글 삭제",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    public static RestrictionType FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "게시물 삭제" => RestrictionType.PostDeletion,
            "댓글 삭제" => RestrictionType.CommentDeletion,
            _ => throw new ArgumentException($"Unknown restriction type: {displayString}", nameof(displayString))
        };
    }
}