using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Comment edit box: owns the edit attachment surface, including the original server-side
// media attachment that stays in place while the user keeps it, and drives the ModifyComment
// request through the host view model's dialog and loading services. A successful edit
// propagates the updated comment through the messenger and raises CommentSent so the host
// window can close itself.
public partial class HistoryEditCommentBoxViewModel : BaseCommentBoxViewModel
{
    private readonly CommentResponseDto _comment;

    // Existing server-side attachment, re-added to the contents when the user keeps it.
    private readonly MediaContent _originalMediaContent;

    public HistoryEditCommentBoxViewModel(CommentResponseDto comment, BaseViewModel dialogBaseViewModel) : base(dialogBaseViewModel)
    {
        _comment = comment;
        _originalMediaContent = comment.Contents.OfType<MediaContent>().FirstOrDefault();
        if (_originalMediaContent == null) return;

        AttachmentImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(_originalMediaContent.MediaId)));
        HasAttachment = true;
    }

    // Sends the edited comment with the given editor contents: a fresh upload becomes an
    // UploadContent with the file bytes, a kept original is re-added as its MediaContent,
    // and a removed attachment is simply left out (the server drops the missing media).
    public override async Task SendCommentAsync(List<BaseContent> contents)
    {
        RemoveEmptyTextContents(contents);

        var files = new Dictionary<string, byte[]>();
        if (AttachmentData is { Length: > 0 } attachmentData)
        {
            contents.Add(new UploadContent { FileName = AttachmentFileName });
            files.Add(AttachmentFileName, attachmentData);
        }
        else if (HasAttachment && _originalMediaContent != null) contents.Add(_originalMediaContent);

        if (contents.Count == 0)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "빈 내용의 댓글은 작성할 수 없습니다"));
            return;
        }

        var result = await BaseViewModel.ExecuteRequestAsync(new ModifyComment(_comment.Id, contents, files), ErrorType.BadRequest, ErrorType.Forbidden);
        if (result.Error == ErrorType.BadRequest || result.Error == ErrorType.Forbidden)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", result.ErrorMessage));
            return;
        }
        else if (result.IsSuccess)
        {
            ClearAttachment();
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<CommentResponseDto>(result.Value));
            RaiseCommentSent();
        }
    }
}
