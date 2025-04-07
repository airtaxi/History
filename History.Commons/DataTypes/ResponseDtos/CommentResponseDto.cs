using History.Commons.DataTypes.Contents;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class CommentResponseDto
{
    public string Id { get; set; }

    public string PostId { get; set; }
    public string UserId { get; set; }

    public List<BaseContent> Contents { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public List<UserResponseDto> LikedUsers { get; set; }
}
