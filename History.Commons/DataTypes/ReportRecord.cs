using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

public class ReportRecord
{
    [BsonId]
    public string Id { get; set; }

    public string AssociatedId { get; set; } // PostId or CommentId
    public List<BaseContent> AssociatedContents { get; set; } // Contents of the post/comment

    public ReportTarget Target { get; set; } // Target of the report (Post or Comment)
    public ReportType Type { get; set; } // Type of the report (e.g., ExplicitContent, CopyrightViolation)

    public string UserId { get; set; } // UserId of the post/comment owner
    public string ReporterId { get; set; } // UserId of the user who reported the post/comment

    public DateTime CreatedAt { get; set; }
}
