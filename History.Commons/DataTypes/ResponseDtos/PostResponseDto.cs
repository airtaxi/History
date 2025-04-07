using History.Commons.DataTypes.Contents;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class PostResponseDto
{
    public string Id { get; set; }

    public string UserId { get; set; }
    public bool IsRepost { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public PostResponseDto ParentPost { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
