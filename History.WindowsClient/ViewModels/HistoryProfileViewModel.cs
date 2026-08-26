using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

public partial class HistoryProfileViewModel : BaseProfileViewModel, IRecipient<ValueChangedMessage<UserResponseDto>>
{
    private readonly string _userId;

    public HistoryProfileViewModel(UserResponseDto user)
    {
        _userId = user.UserId;

        Update(user);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != _userId) return;

        Update(message.Value);
    }

    public async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(_userId));
        if (!result.IsSuccess)
        {
            await ShowMessageDialogAsync(new("오류", "프로필 정보 갱신에 실패하였습니다."));
            return;
        }

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(result.Value));
    }

    private void Update(UserResponseDto user)
    {
        Nickname = user.Nickname;
        Description = user.Description;

        IsMe = user.UserId == CommonShared.UserId;
        IsFriend = user.Friendship?.Status == FriendshipStatus.Accepted;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        IsFavorite = user.IsFavorite;
        IsBlocked = user.Friendship?.Status == FriendshipStatus.Blocked;

        ProfileThumbnailImageSource = user.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId))) : null;
        ProfileImageSource = user.ProfileMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileMediaId))) : null;
        BackgroundImageSource = user.BackgroundMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.BackgroundMediaId))) : null;
    }
}
