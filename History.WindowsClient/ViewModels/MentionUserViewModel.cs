using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels;

public partial class MentionUserViewModel : ObservableObject
{
    public string UserId { get; }
    public string Nickname { get; }
    public bool IsModerator { get; }
    public bool IsAdmin { get; }
    public BitmapImage ProfileThumbnailImageSource { get; }

    public MentionUserViewModel(UserResponseDto user)
    {
        UserId = user.UserId;
        Nickname = user.Nickname;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;

        var mediaUri = CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId);
        ProfileThumbnailImageSource = mediaUri != null ? new BitmapImage(new Uri(mediaUri)) : null;
    }

    public MentionUserViewModel(FriendData.Profile profile)
    {
        UserId = profile.id;
        Nickname = profile.display_name;
        IsModerator = false;
        IsAdmin = false;
        ProfileThumbnailImageSource = profile.profile_thumbnail_url != null ? new BitmapImage(new Uri(profile.profile_thumbnail_url)) : null;
    }
}