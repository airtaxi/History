using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story post detail page: hosts the post view model, the comment list and the
// Kakao Story comment composer.
public partial class KakaoPostPageViewModel : BasePostPageViewModel
{
    public void Initialize(PostData postData)
    {
        if (Post != null) return;

        var kakaoPostViewModel = new KakaoPostViewModel(postData, PostType.Unwrapped, this);
        Post = kakaoPostViewModel;
        CommentBox = new KakaoCommentBoxViewModel(kakaoPostViewModel, this);
    }
}
