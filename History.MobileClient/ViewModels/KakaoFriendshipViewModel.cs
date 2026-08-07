using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoFriendshipViewModel : BaseFriendshipViewModel
{
    private string _relationship;

    public string UserId { get; }
    public string Permalink { get; }

    public KakaoFriendshipViewModel(ShareData.Share share, KakaoInteractionViewModel interactionViewModel = null)
    {
        UserId = share.actor?.id;
        Permalink = share.actor?.permalink;
        _relationship = share.actor?.relationship;
        Nickname = share.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = share.actor?.profile_image_url != null ? new ImageViewModel(share.actor.profile_image_url) : null;
        InteractionViewModel = interactionViewModel;
    }

    public KakaoFriendshipViewModel(CommentLikes commentLike)
    {
        UserId = commentLike.actor?.id;
        Permalink = commentLike.actor?.permalink;
        _relationship = commentLike.actor?.relationship;
        Nickname = commentLike.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = commentLike.actor?.profile_image_url != null ? new ImageViewModel(commentLike.actor.profile_image_url) : null;
    }

    public KakaoFriendshipViewModel(FriendData.Profile profile)
    {
        UserId = profile.id;
        _relationship = profile.relationship;
        Nickname = profile.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = profile.profile_thumbnail_url != null ? new ImageViewModel(profile.profile_thumbnail_url) : null;
    }

    public override bool IsFriendshipImageVisible => UserId != null && UserId != Shared.KakaoUserId;

    public override string FriendshipGlyph => _relationship switch
    {
        "F" => Solid.UserMinus,
        "R" => Solid.UserClock,
        "C" => Solid.UserCheck,
        _ => Solid.UserPlus
    };

    public override Color FriendshipColor => _relationship switch
    {
        "F" => Color.FromRgb(0xbd, 0x00, 0x00),
        "R" or "C" => Colors.ForestGreen,
        _ => Colors.RoyalBlue
    };

    // Mirror HistoryFriendshipViewModel: navigate to the shared post when TargetPostId is set,
    // otherwise open the Kakao Story profile.
    public override async Task HandleTapAsync()
    {
        if (InteractionViewModel?.TargetPostId != null)
        {
            var post = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(InteractionViewModel.TargetPostId));
            if (post != null)
            {
                var postViewModel = new KakaoPostViewModel(post, PostType.Unwrapped);
                var postPage = new PostPage(postViewModel);
                await App.PushAsync(postPage);
            }
            else await App.Page.DisplayAlertAsync("안내", "해당 게시글을 불러올 수 없습니다.", Constants.PromptOk);
        }
        else if (UserId != null)
        {
            var profilePage = new UserPage(UserId, true);
            await App.PushAsync(profilePage);
        }
        else await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
    }

    // Mirror HistoryFriendshipViewModel: friend request/accept/delete from the list row.
    public override async Task HandleFriendshipActionAsync()
    {
        if (UserId == null) return;

        if (_relationship == "F")
        {
            var delete = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님와의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!delete) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteFriend(UserId));
                _relationship = "N";
                OnPropertyChanged(nameof(FriendshipGlyph));
                OnPropertyChanged(nameof(FriendshipColor));
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else if (_relationship == "R")
        {
            var cancel = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!cancel) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(UserId, true));
                _relationship = "N";
                OnPropertyChanged(nameof(FriendshipGlyph));
                OnPropertyChanged(nameof(FriendshipColor));
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청 취소에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else if (_relationship == "C")
        {
            var action = await App.Page.DisplayActionSheetAsync("친구 신청", Constants.PromptCancel, null, "수락", "거절");
            if (action == null || action == Constants.PromptCancel) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.AcceptFriendRequest(UserId, action == "거절"));
                _relationship = action == "거절" ? "N" : "F";
                OnPropertyChanged(nameof(FriendshipGlyph));
                OnPropertyChanged(nameof(FriendshipColor));
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var send = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!send) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(UserId, false));
                _relationship = "R";
                OnPropertyChanged(nameof(FriendshipGlyph));
                OnPropertyChanged(nameof(FriendshipColor));
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }
}
