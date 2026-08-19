using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

public partial class HistoryFriendshipViewModel : BaseFriendshipViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(FriendshipGlyph))]
    [NotifyPropertyChangedFor(nameof(IsFriendshipImageVisible))]
    public partial UserResponseDto User { get; set; }

    public HistoryFriendshipViewModel(UserResponseDto user, HistoryInteractionViewModel interactionViewModel = null)
    {
        User = user;
        InteractionViewModel = interactionViewModel;
        UpdateUserDependentProperties(user);
    }

    public DateTime CreatedAt => User.Friendship?.CreatedAt ?? DateTime.MinValue;

    private void UpdateUserDependentProperties(UserResponseDto user)
    {
        Nickname = user.Nickname;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        ProfileMedia = new ImageViewModel(Utils.GenerateMediaUri(user.ProfileThumbnailMediaId) ?? Constants.DefaultProfileImageFileName);
    }

    public override Color FriendshipColor
    {
        get
        {
            if (User.Friendship == null) return Colors.RoyalBlue;
            else if (User.Friendship.Status == FriendshipStatus.Accepted) return Color.FromRgb(0xbd, 0x00, 0x00);
            else if (User.Friendship.Status == FriendshipStatus.Requested) return Colors.ForestGreen;
            else if (User.Friendship.Status == FriendshipStatus.Waiting) return Colors.ForestGreen;
            else return Color.FromRgb(0x80, 0x80, 0x80);
        }
    }
    public override string FriendshipGlyph
    {
        get
        {
            if (User.Friendship == null) return Solid.UserPlus;
            else if (User.Friendship.Status == FriendshipStatus.Accepted) return Solid.UserMinus;
            else if (User.Friendship.Status == FriendshipStatus.Requested) return Solid.UserClock;
            else if (User.Friendship.Status == FriendshipStatus.Waiting) return Solid.UserCheck;
            else return Solid.UserLock;
        }
    }

    public override bool IsFriendshipImageVisible => User.UserId != Shared.UserId;

    private async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (result.IsSuccess) User = result.Value;
    }

    public override async Task HandleTapAsync()
    {
        if (User == null) return;

        if (InteractionViewModel?.TargetPostId != null)
        {
            var postResult = await App.ExecuteRequestAsync(new GetPost(InteractionViewModel.TargetPostId), ErrorType.Forbidden);
            if (postResult.IsSuccess)
            {
                WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(postResult.Value));
                var postViewModel = new HistoryPostViewModel(postResult.Value, PostType.Unwrapped);
                var postPage = new PostPage(postViewModel);
                await App.PushAsync(postPage);
            }
            else if (postResult.Error == ErrorType.Forbidden) await App.Page.DisplayAlertAsync("안내", "해당 게시글을 읽을 수 있는 권한이 없습니다.", Constants.PromptOk);
        }
        else await App.PushAsync(new BlazorUserPage(User.UserId));
    }

    public override async Task HandleFriendshipActionAsync()
    {
        Result result = null;
        FriendshipStatus? newStatus = null;

        if (User.Friendship == null)
        {
            var send = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (send)
            {
                result = await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Requested;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var delete = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (delete)
            {
                result = await App.ExecuteRequestAsync(new RemoveFriend(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var cancel = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", Constants.PromptYes, Constants.PromptNo);
            if (cancel)
            {
                result = await App.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var accept = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 친구 신청을 수락하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (accept)
            {
                result = await App.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Accepted;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Blocked)
        {
            var unblock = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 차단 조치를 해제하시곘습니까?", Constants.PromptYes, Constants.PromptNo);
            if (unblock)
            {
                result = await App.ExecuteRequestAsync(new UnblockUser(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Ignored)
        {
            var unignore = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 무시 조치를 해제하시곘습니까?", Constants.PromptYes, Constants.PromptNo);
            if (unignore)
            {
                result = await App.ExecuteRequestAsync(new UnignoreUser(User.UserId));
                newStatus = null;
            }
        }

        if (result != null && result.IsSuccess)
        {
            await RefreshAsync();
            await LoginPage.RefreshFriendsAsync();
            WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(User.UserId, newStatus, User));
        }
    }
}
