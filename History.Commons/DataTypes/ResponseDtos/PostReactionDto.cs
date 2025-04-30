using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class PostReactionDto
{
    public UserResponseDto User { get; set; }
    public PostReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
