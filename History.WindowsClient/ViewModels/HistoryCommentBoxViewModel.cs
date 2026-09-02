using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Models;

namespace History.WindowsClient.ViewModels;

// History comment composing: sends CreateComment and refreshes the post so the
// messenger propagates the new comment list to every bound view.
public partial class HistoryCommentBoxViewModel(HistoryPostViewModel postViewModel, BaseViewModel dialogBaseViewModel) : BaseCommentBoxViewModel(dialogBaseViewModel)
{
    private readonly HistoryPostViewModel _postViewModel = postViewModel;

    public override async Task SendCommentAsync(List<BaseContent> contents)
    {
        RemoveEmptyTextContents(contents);
        var files = new Dictionary<string, byte[]>();
        if (AttachmentData is { Length: > 0 } attachmentData)
        {
            contents.Add(new UploadContent { FileName = AttachmentFileName });
            files.Add(AttachmentFileName, attachmentData);
        }

        if (contents.Count == 0)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "빈 내용의 댓글은 작성할 수 없습니다"));
            return;
        }

        var result = await App.ExecuteRequestAsync(new CreateComment(_postViewModel.Post.Id, contents, files), ErrorType.BadRequest, ErrorType.Forbidden);
        if (result.Error == ErrorType.BadRequest || result.Error == ErrorType.Forbidden)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", result.ErrorMessage));
            return;
        }
        else if (result.IsSuccess)
        {
            ClearAttachment();
            await _postViewModel.RefreshAsync();
            RaiseCommentSent();
        }
    }
}