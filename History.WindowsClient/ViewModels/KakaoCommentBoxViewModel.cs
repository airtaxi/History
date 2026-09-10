using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels;

// Kakao Story comment composing: builds the comment payload (stickers and the picker
// image upload as image decorators) and posts it through ReplyToPost, then refreshes
// the post so the messenger propagates the new comment list to every bound view.
public partial class KakaoCommentBoxViewModel(KakaoPostViewModel postViewModel, BaseViewModel dialogBaseViewModel) : BaseCommentBoxViewModel(dialogBaseViewModel)
{
    private readonly KakaoPostViewModel _postViewModel = postViewModel;

    public override async Task SendCommentAsync(List<BaseContent> contents)
    {
        if (_postViewModel.PostData.comment_all_writable == false)
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "댓글을 작성할 수 없는 게시글입니다."));
            return;
        }

        RemoveEmptyTextContents(contents);
        if (contents.Count == 0 && AttachmentData is not { Length: > 0 })
        {
            await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "빈 내용의 댓글은 작성할 수 없습니다"));
            return;
        }

        try
        {
            var payload = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryCommentHelper.BuildCommentPayloadAsync(contents, contents.OfType<StickerContent>().ToList(), AttachmentData, AttachmentFileName));
            if (payload == null)
            {
                await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "첨부한 이미지(webp, heic, heif, avif)를 png로 변환하는 데 실패하여 댓글을 작성할 수 없습니다. 일반적으로 애니메이션이 포함된 webp 이미지이거나 지원되지 않는 형식입니다."));
                return;
            }

            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.ReplyToPost(_postViewModel.PostData.id, payload.Value.Text, payload.Value.Decorators));

            ClearAttachment();
            await _postViewModel.RefreshAsync();
            RaiseCommentSent();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}")); }
    }
}
