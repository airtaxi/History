using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story UP (sympathy) bundled-feed view model: fills the shared repost surface
// (RepostId, RepostedUserNickname, RepostPostfix) from the bundled feed. The bundled feed
// wraps the original activity, so the original post's content renders under the
// "OO님이 UP 했어요" attribution bar.
public partial class KakaoRepostViewModel : KakaoPostViewModel
{
    public KakaoRepostViewModel(PostData postData, PostType postType, BaseViewModel baseViewModel) : base(postData, postType, baseViewModel)
    {
        RepostId = postData.id;
        RepostedUserNickname = postData.bundled_feed?.title_decorators?.FirstOrDefault()?.text;
        RepostPostfix = "님이 UP 했어요";
    }

    protected override void UpdatePost(PostData postData)
    {
        // The bundled feed's original activity is the repost target.
        var original = postData.bundled_feed?.original_activity ?? postData;
        base.UpdatePost(original);
        // Keep the bundle as the canonical post so identity and delete sync use the feed item id.
        CurrentPostData = postData;
        IsRepost = true;
        IsShare = false;
    }
}
