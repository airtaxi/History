using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos;

public class SharedUserDto
{
    public UserResponseDto User { get; set; }
    public DateTime CreatedAt { get; set; }
}
