using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.DiscoveryOptions;

// History implementation of selectable user item view model.
public partial class HistorySelectUserViewModel : BaseSelectUserViewModel
{
    public UserResponseDto User { get; }

    public override string UserId => User.UserId;

    public HistorySelectUserViewModel(UserResponseDto user, bool isSelected = false)
    {
        User = user;
        Nickname = user.Nickname;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        IsFavorite = user.IsFavorite;
        ProfileThumbnailImageSource = user.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId))) : null;
        IsSelected = isSelected;
    }
}
