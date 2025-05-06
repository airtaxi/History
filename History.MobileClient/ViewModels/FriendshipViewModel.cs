using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.ViewModels;

public partial class FriendshipViewModel(UserResponseDto user) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(FriendshipGlyph))]
    public partial UserResponseDto User { get; set; } = user;

    public string Nickname => User.Nickname;
    public bool IsModerator => User.Rank == Rank.Moderator;
    public bool IsAdmin => User.Rank == Rank.Admin;
    public IMediaViewModel ProfileMedia => new ImageViewModel(Utils.GenerateMediaUri(User.ProfileThumbnailMediaId));

    public string FriendshipGlyph
    {
        get
        {
            if (User.Friendship == null) return MaterialSharp.Person_add;
            else if (User.Friendship.Status == FriendshipStatus.Accepted) return MaterialSharp.Person_remove;
            else if (User.Friendship.Status == FriendshipStatus.Requested) return MaterialSharp.Person_cancel;
            else if (User.Friendship.Status == FriendshipStatus.Waiting) return MaterialSharp.Person_check;
            else return MaterialSharp.Person_alert;
        }
    }

    private async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (result.IsSuccess) User = result.Value;
    }

    public async Task HandleTapAsync()
    {
        if (User == null) return;
        await App.PushModalAsync(new UserPage(User.UserId));
    }

    public async Task HandleFriendshipActionAsync()
    {
        if (User.Friendship == null)
        {
            var result = await App.Page.DisplayAlert("안내", $"{Nickname}에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var result = await App.Page.DisplayAlert("안내", $"{Nickname}와의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new RemoveFriend(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var result = await App.Page.DisplayAlert("안내", $"{Nickname}에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var result = await App.Page.DisplayAlert("안내", $"{Nickname}의 친구 신청을 수락하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
        }

        await RefreshAsync();
    }
}
