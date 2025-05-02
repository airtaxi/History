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
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial CommentResponseDto Comment { get; set; } = comment;

    public IMediaViewModel ProfileMedia => Comment.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public string Nickname => Comment.User.Nickname;

    public List<IContentViewModel> Contents => Utils.GenerateContentViewModels(Comment.Contents, false);

    public DateTime CreatedAt => Comment.CreatedAt;
    public DateTime? ModifiedAt => Comment.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);
}
