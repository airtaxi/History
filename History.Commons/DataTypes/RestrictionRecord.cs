using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes;

public class RestrictionRecord
{
    [BsonId]
    public string Id { get; set; }

    public RestrictionType Type { get; set; }

    public string AssociatedId { get; set; } // PostId or CommentId
    public List<BaseContent> AssociatedContents { get; set; } // Contents of the post/comment
    public DateTime AssociatedCreatedAt { get; set; } // Creation time of the associated post/comment
    public DateTime? AssociatedModifiedAt { get; set; } // Creation time of the associated post/comment

    public string UserId { get; set; } // UserId of the post/comment owner
    public string ModeratorId { get; set; } // UserId of the moderator who applied the restriction
    public string Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
