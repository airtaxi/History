using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class MentionUserViewModel
{
    public string UserId { get; }
    public string Nickname { get; }
    public bool IsModerator { get; }
    public bool IsAdmin { get; }
    public IMediaViewModel ProfileMedia { get; }

    public MentionUserViewModel(UserResponseDto user)
    {
        UserId = user.UserId;
        Nickname = user.Nickname;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        ProfileMedia = new ImageViewModel(Utils.GenerateMediaUri(user.ProfileThumbnailMediaId) ?? Constants.DefaultProfileImageFileName);
    }

    public MentionUserViewModel(FriendData.Profile profile)
    {
        UserId = profile.id;
        Nickname = profile.display_name;
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = profile.profile_thumbnail_url != null ? new ImageViewModel(profile.profile_thumbnail_url) : null;
    }
}
