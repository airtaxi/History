using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class CreateReportRecordRequestDto
{
    public ReportType Type { get; set; } // Type of the report (e.g., ExplicitContent, CopyrightViolation)
    public ReportTarget Target { get; set; } // Target of the report (Post or Comment)

    public string AssociatedId { get; set; } // PostId or CommentId
}
