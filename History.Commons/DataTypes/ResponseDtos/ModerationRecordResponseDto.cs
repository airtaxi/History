using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class ModerationRecordResponseDto()
{
    public string Id { get; set; }

    public RestrictionType Type { get; set; }

    public List<BaseContent> AssociatedContents { get; set; } // Contents of the post/comment

    public UserResponseDto User { get; set; } // UserId of the post/comment owner
    public UserResponseDto Moderator { get; set; } // UserId of the moderator who applied the restriction

    public string Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public ModerationRecordResponseDto(ModerationRecord record) : this()
    {
        Id = record.Id;
        Type = record.RestrictionType;
        AssociatedContents = record.AssociatedContents;

        Reason = record.Reason;
        CreatedAt = record.CreatedAt;
    }
}
