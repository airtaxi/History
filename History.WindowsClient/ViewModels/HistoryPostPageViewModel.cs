using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels;

// History post detail page: hosts the post view model and the History comment box.
public partial class HistoryPostPageViewModel : BasePostPageViewModel
{
    public void Initialize(PostResponseDto post)
    {
        if (Post != null) return;

        var historyPostViewModel = new HistoryPostViewModel(post, PostType.Unwrapped, this);
        Post = historyPostViewModel;
        CommentBox = new HistoryCommentBoxViewModel(historyPostViewModel, this);
    }
}