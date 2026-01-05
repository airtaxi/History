using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

public partial class MentionUserViewModel(UserResponseDto user)
{
    public string UserId => user.UserId;

    public string Nickname => user.Nickname;

    public bool IsModerator => user.Rank == Rank.Moderator;
    public bool IsAdmin => user.Rank == Rank.Admin;

    public IMediaViewModel ProfileMedia => new ImageViewModel(Utils.GenerateMediaUri(user.ProfileThumbnailMediaId) ?? Constants.DefaultProfileImageFileName);
}
