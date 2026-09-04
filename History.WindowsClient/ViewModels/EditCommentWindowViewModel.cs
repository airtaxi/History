using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.WindowsClient.ViewModels;

// Comment edit state for EditCommentWindow. Prefills the editor contents from the comment
// and hosts the edit comment box, which owns the attachment surface and the ModifyComment
// request; the dialog, picker, and loading events are fulfilled by the window code-behind.
public sealed partial class EditCommentWindowViewModel : BaseViewModel
{
    // Editor prefill: the original contents minus the media attachment, which the preview handles.
    public List<BaseContent> EditorContents { get; }

    public BaseCommentBoxViewModel CommentBox { get; }

    public EditCommentWindowViewModel(CommentResponseDto comment)
    {
        EditorContents = [.. comment.Contents.Where(content => content is not MediaContent)];
        CommentBox = new HistoryEditCommentBoxViewModel(comment, this);
    }
}
