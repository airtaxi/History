using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story comment edit box: owns the original comment image while the user keeps it,
// rebuilds the comment payload from the editor contents, and drives the EditComment request
// through the host view model's dialog and loading services. A successful edit propagates
// the updated comment through the messenger and raises CommentSent so the host window can
// close itself.
public partial class KakaoEditCommentBoxViewModel : BaseCommentBoxViewModel
{
    private readonly Comment _comment;
    private readonly string _postId;

    public KakaoEditCommentBoxViewModel(Comment comment, string postId, BaseViewModel dialogBaseViewModel) : base(dialogBaseViewModel)
    {
        _comment = comment;
        _postId = postId;

        // The comment image is stored as a media decorator; a displayable URL marks the
        // attachment the user can keep, replace, or remove.
        var originalMedia = comment.decorators?.FirstOrDefault(decorator => decorator.media?.media_path != null)?.media;
        var previewUrl = originalMedia?.thumbnail_url ?? originalMedia?.url;
        if (previewUrl == null) return;

        AttachmentImageSource = new BitmapImage(new Uri(previewUrl));
        HasAttachment = true;
    }

    public override async Task SendCommentAsync(List<BaseContent> contents)
    {
        RemoveEmptyTextContents(contents);
        if (contents.Count == 0 && !HasAttachment)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "빈 내용의 댓글은 작성할 수 없습니다"));
            return;
        }

        try
        {
            var payload = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryCommentHelper.BuildCommentPayloadAsync(contents, contents.OfType<StickerContent>().ToList(), AttachmentData, AttachmentFileName));
            if (payload == null)
            {
                await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "첨부한 이미지(webp, heic, heif, avif)를 png로 변환하는 데 실패하여 댓글을 수정할 수 없습니다. 일반적으로 애니메이션이 포함된 webp 이미지이거나 지원되지 않는 형식입니다."));
                return;
            }

            // The original image is preserved server-side only while the user keeps it; a
            // replaced or removed attachment leaves it out of the request.
            var preserveOldImage = HasAttachment && AttachmentData is not { Length: > 0 };
            var comment = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.EditComment(_comment, _postId, payload.Value.Decorators, payload.Value.Text, preserveOldImage));
            if (comment == null) return;

            ClearAttachment();
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Comment>(comment));
            RaiseCommentSent();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}")); }
    }
}
