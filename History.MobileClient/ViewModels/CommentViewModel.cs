using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class CommentViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(HasLikes))]
    [NotifyPropertyChangedFor(nameof(LikesCount))]
    [NotifyPropertyChangedFor(nameof(LikedUsers))]
    [NotifyPropertyChangedFor(nameof(Liked))]
    [NotifyPropertyChangedFor(nameof(CommentLikeFontFamily))]
    [NotifyPropertyChangedFor(nameof(CommentLikeColor))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial CommentResponseDto Comment { get; set; }

    public IMediaViewModel ProfileMedia => Comment.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public string Nickname => Comment.User.Nickname;

    public List<IContentViewModel> Contents => Utils.GenerateContentViewModels(Comment.Contents, false);

    public bool HasLikes => Comment.LikedUsers.Count > 0;
    public int LikesCount => Comment.LikedUsers.Count;
    public List<ProfileViewModel> LikedUsers => Comment.LikedUsers.Select(u => new ProfileViewModel(u)).ToList();

    public bool Liked => Comment.LikedUsers.Any(x => x.UserId == Shared.UserId);
    public string CommentLikeFontFamily => Liked ? "FASolid" : "FARegular";
    public Color CommentLikeColor => Liked ? Color.FromRgb(0xeb, 0x55, 0x27) : Color.FromRgb(0x80, 0x80, 0x80);

    [RelayCommand]
    public async Task LikeAsync()
    {
        var commentResult = await App.ExecuteRequestAsync(new HandleCommentLike(Comment.Id));
        if (commentResult.IsSuccess) Comment = commentResult.Value;
        else return;
    }

    public DateTime CreatedAt => Comment.CreatedAt;
    public DateTime? ModifiedAt => Comment.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

    public CommentViewModel(CommentResponseDto comment)
    {
        Comment = comment;
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, (r,m) =>
        {
            if (m.Value.Id != Comment.Id) return;

            Comment = m.Value;
        });
    }
}
