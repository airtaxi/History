using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

public partial class FriendshipViewModel(UserResponseDto user, InteractionViewModel interactionViewModel = null) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(FriendshipGlyph))]
    [NotifyPropertyChangedFor(nameof(IsFriendshipImageVisible))]
    [NotifyPropertyChangedFor(nameof(IsInteractionAvailable))]
    public partial UserResponseDto User { get; set; } = user;

    public string Nickname => User.Nickname;
    public bool IsModerator => User.Rank == Rank.Moderator;
    public bool IsAdmin => User.Rank == Rank.Admin;
    public DateTime CreatedAt => User.Friendship.CreatedAt;
    public IMediaViewModel ProfileMedia => new ImageViewModel(Utils.GenerateMediaUri(User.ProfileThumbnailMediaId) ?? Constants.DefaultProfileImageFileName);

    public Color FriendshipColor
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
    public string FriendshipGlyph
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

    public bool IsFriendshipImageVisible => User.UserId != Shared.UserId;

    public bool IsInteractionAvailable => InteractionViewModel != null;
    public InteractionViewModel InteractionViewModel { get; } = interactionViewModel;

    private async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (result.IsSuccess) User = result.Value;
    }

    [RelayCommand]
    private async Task HandleTapAsync()
    {
        if (User == null) return;

        if (InteractionViewModel?.TargetPostId != null)
        {
            var postResult = await App.ExecuteRequestAsync(new GetPost(InteractionViewModel.TargetPostId), ErrorType.Forbidden);
            if (postResult.IsSuccess)
            {
                var postViewModel = new PostViewModel(postResult.Value, PostType.Unwrapped);
                var postPage = new PostPage(postViewModel);
                await App.PushAsync(postPage);
            }
            else if (postResult.Error == ErrorType.Forbidden) await App.Page.DisplayAlertAsync("안내", "해당 게시글을 읽을 수 있는 권한이 없습니다.", Constants.PromptOk);
        }
        else await App.PushAsync(new UserPage(User.UserId));
    }

    [RelayCommand]
    private async Task HandleFriendshipActionAsync()
    {
        if (User.Friendship == null)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new RemoveFriend(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 친구 신청을 수락하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Blocked)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 차단 조치를 해제하시곘습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new UnblockUser(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Ignored)
        {
            var result = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님의 무시 조치를 해제하시곘습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new UnignoreUser(User.UserId));
        }

        await RefreshAsync();
    }
}
