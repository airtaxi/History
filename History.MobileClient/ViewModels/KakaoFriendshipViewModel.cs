using History.Commons;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoFriendshipViewModel : BaseFriendshipViewModel
{
    public string UserId { get; }
    public string Permalink { get; }

    public KakaoFriendshipViewModel(ShareData.Share share)
    {
        UserId = share.actor?.id;
        Permalink = share.actor?.permalink;
        Nickname = share.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = share.actor?.profile_image_url != null ? new ImageViewModel(share.actor.profile_image_url) : null;
    }

    public KakaoFriendshipViewModel(CommentLikes commentLike)
    {
        UserId = commentLike.actor?.id;
        Permalink = commentLike.actor?.permalink;
        Nickname = commentLike.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = commentLike.actor?.profile_image_url != null ? new ImageViewModel(commentLike.actor.profile_image_url) : null;
    }

    // Kakao Story profile pages are not implemented yet.
    public override async Task HandleTapAsync() => await App.Page.DisplayAlertAsync("안내", "카카오스토리 프로필 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
}
