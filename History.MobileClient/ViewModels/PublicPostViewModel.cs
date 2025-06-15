using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class PublicPostViewModel(PostResponseDto post) : PostViewModel(post, false);
