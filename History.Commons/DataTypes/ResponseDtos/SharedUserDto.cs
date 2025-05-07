using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class SharedUserDto
{
    public UserResponseDto User { get; set; }
    public DateTime SharedAt { get; set; }
    public bool IsRepost { get; set; }
}
