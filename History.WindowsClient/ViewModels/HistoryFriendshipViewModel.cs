using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace History.WindowsClient.ViewModels;

public partial class HistoryFriendshipViewModel : BaseFriendshipViewModel, IRecipient<ValueChangedMessage<UserResponseDto>>
{
    public UserResponseDto User { get; }

    public HistoryFriendshipViewModel(UserResponseDto user)
    {
        User = user;

        Update(user);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != User.UserId) return;

        Update(message.Value);
    }

    public async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (!result.IsSuccess)
        {
            await ShowMessageDialogAsync(new("오류", "친구 정보 갱신에 실패하였습니다."));
            return;
        }

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(result.Value));
    }

    private void Update(UserResponseDto user)
    {
        Nickname = user.Nickname;
        
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        IsFavorite = user.IsFavorite;

        ProfileThumbnailImageSource = user.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId))) : null;
        ProfileImageSource = user.ProfileMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileMediaId))) : null;

        FriendshipGlyph = GetFriendshipGlyph();
        FriendshipForeground = new SolidColorBrush(GetFriendshipColor());
    }

    private Color GetFriendshipColor()
    {
        if (User.Friendship == null) return Colors.RoyalBlue;
        else if (User.Friendship.Status == FriendshipStatus.Accepted) return Color.FromArgb(0xff, 0xbd, 0x00, 0x00);
        else if (User.Friendship.Status == FriendshipStatus.Requested) return Colors.ForestGreen;
        else if (User.Friendship.Status == FriendshipStatus.Waiting) return Colors.ForestGreen;
        else if (User.Friendship.Status == FriendshipStatus.Ignored) return Color.FromArgb(0xff, 0xbd, 0x00, 0x00);
        else if (User.Friendship.Status == FriendshipStatus.Blocked) return Color.FromArgb(0xff, 0xbd, 0x00, 0x00);
        else return Color.FromArgb(0xff, 0x80, 0x80, 0x80);
    }

    private string GetFriendshipGlyph()
    {
        if (User.Friendship == null) return "\uE8FA";
        else if (User.Friendship.Status == FriendshipStatus.Accepted) return "\uF69B";
        else if (User.Friendship.Status == FriendshipStatus.Requested) return "\uEFA9";
        else if (User.Friendship.Status == FriendshipStatus.Waiting) return "\uEFA9";
        else if (User.Friendship.Status == FriendshipStatus.Ignored)  return "\uE8F8";
        else if (User.Friendship.Status == FriendshipStatus.Blocked)  return "\uE8F8";
        else return "\uE716";
    }
}
