using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class CommentViewModel(CommentResponseDto comment) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    public partial CommentResponseDto Comment { get; set; } = comment;

    public string Nickname => Comment.User.Nickname;
    public List<BaseContent> Contents => Comment.Contents;
}
