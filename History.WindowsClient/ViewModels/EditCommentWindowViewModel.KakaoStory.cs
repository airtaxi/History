using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story editing surface of EditCommentWindow: prefills the editor from the comment
// decorators (the image decorator stays in the box attachment surface) and hosts the
// Kakao edit comment box.
public sealed partial class EditCommentWindowViewModel : BaseViewModel
{
    public EditCommentWindowViewModel(Comment comment, string postId)
    {
        // Like responses may omit the decorators; fall back to the plain text so the
        // prefill matches what the comment list renders.
        var quoteDatas = comment.decorators is { Count: > 0 } ? comment.decorators : KakaoStoryUtils.GetQuoteDataFromString(comment.text ?? string.Empty);
        EditorContents = [.. KakaoStoryUtils.ConvertToBaseContents(quoteDatas).Where(content => content is not MediaContent)];
        CommentBox = new KakaoEditCommentBoxViewModel(comment, postId, this);
    }
}
