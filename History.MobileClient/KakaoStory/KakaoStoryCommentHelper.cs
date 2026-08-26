using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.KakaoStory;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Graphics.Platform;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.KakaoStory;

// Shared Kakao Story comment payload builder used by both comment creation (PostPage)
// and comment editing (EditCommentPage). Stickers are uploaded as images when possible
// (webp is converted to PNG since Kakao Story does not accept webp); otherwise they
// degrade to "(스티커)" text. The picker image is uploaded and placed first so the API
// renders the first decorator as the comment image. Profile mentions are converted
// from the editor contents (type="profile" with the friend id), not parsed from text.
public partial class KakaoStoryCommentHelper : CommonKakaoStoryCommentHelper
{
    /// <summary>
    /// Builds the Kakao Story comment payload. Returns null when the picker image
    /// (webp) cannot be converted to PNG, so the caller can abort the comment.
    /// </summary>
    public static async Task<(List<QuoteData> Decorators, string Text)?> BuildCommentPayloadAsync(List<BaseContent> contents, List<StickerContent> stickerContents, MediaAttachmentViewModel attachmentViewModel)
    {
        // The editor appends a '\n' after a sticker image token so it renders on its own line.
        // Stickers become image decorators, so that newline must not leak into the following
        // text decorator (sticker + body would post "\nbody" with an empty first line) — strip
        // the leading newlines from the text that follows each sticker, and strip trailing
        // whitespace-only text contents (sticker alone) before building the payload.
        TrimNewlinesAfterStickers(contents);
        TrimTrailingWhitespaceTextContents(contents);

        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromContents(contents);

        // Stickers resolve to an uploaded image when possible; failed uploads are ignored.
        var imageQuoteDatas = new List<QuoteData>();
        foreach (var stickerContent in stickerContents)
        {
            if (stickerContent.StickerMediaId == null) continue;

            var imageData = await CommonUtils.GetStickerImageDataAsync(stickerContent.StickerMediaId);
            if (imageData.Length == 0) continue;

            // All History stickers are webp, which KakaoStory does not accept.
            // Convert to PNG before uploading, same as the EditPostPage media upload flow.
            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"comment_sticker_{Guid.NewGuid():N}.png");
            try
            {
                using var stream = new MemoryStream(imageData);
                using var image = PlatformImage.FromStream(stream);
                if (image == null) continue;

                using var saveStream = File.Create(tempFilePath);
                await image.SaveAsync(saveStream, ImageFormat.Png);
            }
            catch
            {
                try { File.Delete(tempFilePath); } catch { }
                continue;
            }

            try
            {
                var uploadedImage = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImageProp(tempFilePath));
                imageQuoteDatas.Add(new QuoteData
                {
                    type = "image",
                    text = "(Image) ",
                    media_path = BuildKakaoMediaPath(uploadedImage)
                });
            }
            finally { try { File.Delete(tempFilePath); } catch { } }
        }

        // The picker image goes first; the API renders the first decorator as the comment image.
        if (attachmentViewModel != null && attachmentViewModel.FilePath != null)
        {
            string filePath = attachmentViewModel.FilePath;
            var isWebp = attachmentViewModel.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
            if (isWebp)
            {
                // KakaoStory does not accept webp; convert to PNG before uploading,
                // same as the sticker flow above.
                var fileName = Path.GetFileNameWithoutExtension(filePath) + ".png";
                filePath = Path.GetTempPath() + "c_" + fileName;
                using var stream = File.OpenRead(attachmentViewModel.FilePath);
                using var image = PlatformImage.FromStream(stream);
                if (image == null) return null; // Conversion failed; abort the comment.
                using var saveStream = File.Create(filePath);
                await image.SaveAsync(saveStream, ImageFormat.Png);
            }

            try
            {
                var uploadedImage = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImageProp(filePath));
                imageQuoteDatas.Insert(0, new QuoteData
                {
                    type = "image",
                    text = "(Image) ",
                    media_path = BuildKakaoMediaPath(uploadedImage)
                });
            }
            finally
            {
                if (filePath != attachmentViewModel.FilePath)
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
        }

        var decorators = imageQuoteDatas.Concat(quoteDatas).ToList();
        // The API expects the plain text to mirror the decorators (KSMP pattern: space-joined decorator texts).
        var plainText = string.Join(' ', decorators.Select(x => x.text));
        return (decorators, plainText);
    }
}
