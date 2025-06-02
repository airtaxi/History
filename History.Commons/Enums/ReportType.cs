using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ReportType>))]
public enum ReportType
{
    ExplicitContent,
    CopyrightViolation,
    IllegalContent,
    Other
}

public static class ReportTypeExtensions
{
    public static string ToDisplayString(this ReportType type)
    {
        return type switch
        {
            ReportType.ExplicitContent => "성인물",
            ReportType.CopyrightViolation => "저작권 위반",
            ReportType.IllegalContent => "불법 콘텐츠",
            ReportType.Other => "기타",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static ReportType FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "성인물" => ReportType.ExplicitContent,
            "저작권 위반" => ReportType.CopyrightViolation,
            "불법 콘텐츠" => ReportType.IllegalContent,
            "기타" => ReportType.Other,
            _ => throw new ArgumentException($"Unknown report type: {displayString}", nameof(displayString))
        };
    }
}