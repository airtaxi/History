using History.Commons;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

// Kakao Story UP (sympathy) bundled-feed view model: fills the shared repost surface
// (RepostId, RepostedUserNickname) from the bundled feed, mirroring HistoryRepostViewModel.
// The bundled feed wraps the original activity; this VM renders the original post's
// content with the shared RepostTemplate attribution bar ("OO님이 리포스트 했어요").
public partial class KakaoRepostViewModel : KakaoPostViewModel
{
    public KakaoRepostViewModel(PostData postData, PostType postType = PostType.Timeline) : base(postData, postType)
    {
        RepostId = postData.id;
        RepostedUserNickname = postData.bundled_feed?.title_decorators?.FirstOrDefault()?.text;
    }

    protected override void UpdatePost(PostData postData)
    {
        // The bundled feed's original activity is the repost target (WPF pattern).
        var original = postData.bundled_feed?.original_activity ?? postData;
        base.UpdatePost(original);
        // Keep the bundle as the canonical post so GetPost(bundle id) opens the detail (WPF pattern).
        CurrentPostData = postData;
        IsRepost = true;
        IsShare = false;
    }

    public override async Task HandleRepostedUserTap() => await App.Page.DisplayAlertAsync("안내", "카카오스토리 프로필 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
}
