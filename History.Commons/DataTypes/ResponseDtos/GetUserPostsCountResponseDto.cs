using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class GetUserPostsCountResponseDto()
{
    public long Count { get; }

    public GetUserPostsCountResponseDto(long count) : this() => Count = count;
}