using History.Commons;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoFriendshipViewModel : BaseFriendshipViewModel
{
    public string UserId { get; }
    public string Permalink { get; }

    public KakaoFriendshipViewModel(ShareData.Share share, KakaoInteractionViewModel interactionViewModel = null)
    {
        UserId = share.actor?.id;
        Permalink = share.actor?.permalink;
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
        Nickname = commentLike.actor?.display_name ?? "알 수 없는 사용자";
        IsModerator = false;
        IsAdmin = false;
        ProfileMedia = commentLike.actor?.profile_image_url != null ? new ImageViewModel(commentLike.actor.profile_image_url) : null;
    }

    // Mirror HistoryFriendshipViewModel: navigate to the shared post when TargetPostId is set,
    // otherwise show the profile-not-supported notice.
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
}
