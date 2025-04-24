using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public class PostViewModel(PostResponseDto post)
{
    public PostResponseDto Post { get; } = post;
}
