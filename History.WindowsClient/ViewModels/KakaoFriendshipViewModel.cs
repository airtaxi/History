using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story friendship row surface: friend entries, invitations, search results and
// blocked users. The relationship code drives the action button: F friend, R sent
// request, C received request, B blocked, anything else means no relation.
public partial class KakaoFriendshipViewModel : BaseFriendshipViewModel
{
    private readonly BaseViewModel _baseViewModel;
    private string _relationship;

    public string UserId { get; }

    // The current user's own row offers neither friendship actions nor the favorite badge.
    public override bool IsFriendshipActionVisible => UserId != CommonShared.KakaoUserId;
    public override bool IsFavoriteVisible => IsFavorite && UserId != CommonShared.KakaoUserId;

    public KakaoFriendshipViewModel(ShareData.Share share, BaseViewModel baseViewModel, KakaoInteractionViewModel interactionViewModel = null)
    {
        _baseViewModel = baseViewModel;
        InteractionViewModel = interactionViewModel;
        UserId = share.actor?.id;
        _relationship = share.actor?.relationship;
        Nickname = share.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        IsFavorite = share.actor?.is_favorite == true;
        SetProfileImage(share.actor?.profile_thumbnail_url ?? share.actor?.profile_image_url);
        UpdateFriendshipVisual();
    }

    public KakaoFriendshipViewModel(CommentLikes commentLike, BaseViewModel baseViewModel, KakaoInteractionViewModel interactionViewModel = null)
    {
        _baseViewModel = baseViewModel;
        InteractionViewModel = interactionViewModel;
        UserId = commentLike.actor?.id;
        _relationship = commentLike.actor?.relationship;
        Nickname = commentLike.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        IsFavorite = commentLike.actor?.is_favorite == true;
        SetProfileImage(commentLike.actor?.profile_thumbnail_url ?? commentLike.actor?.profile_image_url);
        UpdateFriendshipVisual();
    }

    public KakaoFriendshipViewModel(FriendData.Profile profile, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        UserId = profile.id;
        _relationship = profile.relationship;
        Nickname = profile.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        IsFavorite = profile.is_favorite;
        SetProfileImage(profile.profile_thumbnail_url);
        UpdateFriendshipVisual();
    }

    public KakaoFriendshipViewModel(SearchData.SearchResult searchResult, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        UserId = searchResult.object_id;
        _relationship = searchResult.relation?.friend;
        Nickname = searchResult.name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        SetProfileImage(searchResult.object_image_url);
        UpdateFriendshipVisual();
    }

    public KakaoFriendshipViewModel(InvitationData.Invitation invitation, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        UserId = invitation.user_id;
        _relationship = invitation.type == "received" ? "C" : "R";
        Nickname = invitation.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        SetProfileImage(invitation.profile_thumbnail_url ?? invitation.profile_image_url);
        UpdateFriendshipVisual();
    }

    public KakaoFriendshipViewModel(ProfileData.Profile profile, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        UserId = profile.id;
        _relationship = "B";
        Nickname = profile.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        IsFavorite = profile.is_favorite;
        SetProfileImage(profile.profile_thumbnail_url ?? profile.profile_image_url);
        UpdateFriendshipVisual();
    }

    private void SetProfileImage(string imageUrl)
    {
        var imageSource = imageUrl != null ? new BitmapImage(new Uri(imageUrl)) : null;
        ProfileThumbnailImageSource = imageSource;
        ProfileImageSource = imageSource;
    }

    // The friendship action glyphs reuse the History friendship glyphs for the same states.
    private void UpdateFriendshipVisual()
    {
        FriendshipGlyph = _relationship switch
        {
            "F" => "\uF69B",
            "R" or "C" => "\uEFA9",
            "B" => "\uE8F8",
            _ => "\uE8FA",
        };
        FriendshipForeground = new SolidColorBrush(_relationship switch
        {
            "F" => Color.FromArgb(0xFF, 0xBD, 0x00, 0x00),
            "R" or "C" => Colors.ForestGreen,
            "B" => Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
            _ => Colors.RoyalBlue,
        });
    }

    public override async Task HandleTapAsync()
    {
        if (InteractionViewModel?.TargetPostId == null)
        {
            HandleProfileTap();
            return;
        }

        var post = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(InteractionViewModel.TargetPostId));
        if (post == null)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("안내", "해당 게시글을 불러올 수 없습니다."));
            return;
        }

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));
        _baseViewModel.RequestNavigation(typeof(PostPage), post);
    }

    public override void HandleProfileTap()
    {
        if (UserId == null) return;

        _baseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(UserId));
    }

    public override async Task HandleFriendshipActionAsync()
    {
        if (UserId == null) return;

        if (_relationship == "F")
        {
            var delete = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (delete != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteFriend(UserId));
                _relationship = "N";
                UpdateFriendshipVisual();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 삭제에 실패하였습니다.\n{exception.Message}")); }
        }
        else if (_relationship == "B")
        {
            var unban = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"정말로 {Nickname}님의 차단을 해제하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (unban != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UnbanProfile(UserId));
                _relationship = "N";
                UpdateFriendshipVisual();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"차단 해제에 실패하였습니다.\n{exception.Message}")); }
        }
        else if (_relationship == "R")
        {
            var cancel = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (cancel != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(UserId, true));
                _relationship = "N";
                UpdateFriendshipVisual();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청 취소에 실패하였습니다.\n{exception.Message}")); }
        }
        else if (_relationship == "C")
        {
            var action = await _baseViewModel.ShowSelectionDialogAsync("친구 신청", ["수락", "거절"]);
            if (action == null) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.AcceptFriendRequest(UserId, action == "거절"));
                _relationship = action == "거절" ? "N" : "F";
                UpdateFriendshipVisual();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청 처리에 실패하였습니다.\n{exception.Message}")); }
        }
        else
        {
            var send = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (send != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(UserId, false));
                _relationship = "R";
                UpdateFriendshipVisual();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청에 실패하였습니다.\n{exception.Message}")); }
        }
    }
}
