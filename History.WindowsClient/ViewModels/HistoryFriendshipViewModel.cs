using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Pages;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace History.WindowsClient.ViewModels;

public partial class HistoryFriendshipViewModel : BaseFriendshipViewModel, IRecipient<ValueChangedMessage<UserResponseDto>>
{
    public UserResponseDto User { get; }

    private readonly BaseViewModel _baseViewModel;

    public HistoryFriendshipViewModel(UserResponseDto user, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
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
        var result = await _baseViewModel.ExecuteRequestAsync(new GetUser(User.UserId));
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

    public override void HandleProfileTap() => _baseViewModel.RequestNavigation(typeof(ProfilePage), User.UserId);

    public override async Task HandleFriendshipActionAsync()
    {
        Result result = null;
        FriendshipStatus? newStatus = null;

        if (User.Friendship == null)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Requested;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new RemoveFriend(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님의 친구 신청을 수락하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Accepted;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Blocked)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님의 차단 조치를 해제하시곘습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new UnblockUser(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Ignored)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님의 무시 조치를 해제하시곘습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new UnignoreUser(User.UserId));
                newStatus = null;
            }
        }

        if (result != null && result.IsSuccess)
        {
            await RefreshAsync();
            WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(User.UserId, newStatus, User));
        }
    }
}
