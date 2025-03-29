using History.Commons.DataTypes.Content;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.Dto;

public class FeedResponseDto
{
    public string Id { get; set; }

    public string AuthorUserId { get; set; }
    public bool IsRepost { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public FeedResponseDto ParentFeed { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
