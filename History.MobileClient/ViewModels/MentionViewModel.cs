using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.MobileClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.ViewModels;

public partial class MentionViewModel(UserResponseDto user)
{
    public string UserId => user.UserId;

    public string Nickname => user.Nickname;

    public IMediaViewModel ProfileMedia => new ImageViewModel(Utils.GenerateMediaUri(user.ProfileThumbnailMediaId));
}
