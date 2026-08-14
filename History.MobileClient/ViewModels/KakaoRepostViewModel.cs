using History.Commons;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

// Kakao Story UP (sympathy) bundled-feed view model: fills the shared repost surface
// (RepostId, RepostedUserNickname, RepostPostfix) from the bundled feed, mirroring HistoryRepostViewModel.
// The bundled feed wraps the original activity; this VM renders the original post's
// content with the shared RepostTemplate attribution bar ("OO님이 UP 했어요").
public partial class KakaoRepostViewModel : KakaoPostViewModel
{
    public KakaoRepostViewModel(PostData postData, PostType postType = PostType.Timeline) : base(postData, postType)
    {
        RepostId = postData.id;
        RepostedUserNickname = postData.bundled_feed?.title_decorators?.FirstOrDefault()?.text;
        RepostPostfix = "님이 UP 했어요";
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

    public override async Task HandleRepostedUserTap()
    {
        var actorId = CurrentPostData.bundled_feed?.original_activity?.actor?.id;
        if (actorId == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        var profilePage = new BlazorUserPage(actorId, true);
        await App.PushAsync(profilePage);
    }
}
