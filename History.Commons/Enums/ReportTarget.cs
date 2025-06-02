using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ReportTarget>))]
public enum ReportTarget
{
    Post,
    Comment
}

public static class ReportTargetExtensions
{
    public static string ToDisplayString(this ReportTarget target)
    {
        return target switch
        {
            ReportTarget.Post => "게시물",
            ReportTarget.Comment => "댓글",
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }
    public static ReportTarget FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "게시물" => ReportTarget.Post,
            "댓글" => ReportTarget.Comment,
            _ => throw new ArgumentException($"Unknown report target: {displayString}", nameof(displayString))
        };
    }
}