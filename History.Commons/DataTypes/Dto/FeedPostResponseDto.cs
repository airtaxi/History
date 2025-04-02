using History.Commons.DataTypes.Content;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.Dto;

public class FeedPostResponseDto : PostResponseDto
{
    public Comment LastComment { get; set; }
}
