using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class ReportRecordResponseDto()
{
    public string Id { get; set; }

    public List<BaseContent> AssociatedContents { get; set; } // Contents of the post/comment

    public ReportTarget Target { get; set; } // Target of the report (Post or Comment)
    public ReportType Type { get; set; } // Type of the report (e.g., ExplicitContent, CopyrightViolation)

    public UserResponseDto User { get; set; } // UserId of the post/comment owner
    public UserResponseDto Reporter { get; set; } // UserId of the user who reported the post/comment

    public DateTime CreatedAt { get; set; }

    public ReportRecordResponseDto(ReportRecord record) : this()
    {
        Id = record.Id;

        AssociatedContents = record.AssociatedContents;

        Target = record.Target;
        Type = record.Type;

        CreatedAt = record.CreatedAt;
    }
}
