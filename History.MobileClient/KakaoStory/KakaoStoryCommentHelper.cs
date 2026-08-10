using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Graphics.Platform;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.KakaoStory;

// Shared Kakao Story comment payload builder used by both comment creation (PostPage)
// and comment editing (EditCommentPage). Stickers are uploaded as images when possible
// (webp is converted to PNG since Kakao Story does not accept webp); otherwise they
// degrade to "(스티커)" text. The picker image is uploaded and placed first so the API
// renders the first decorator as the comment image. Profile mentions are converted
// from the editor contents (type="profile" with the friend id), not parsed from text.
public static class KakaoStoryCommentHelper
{
    public static async Task<(List<QuoteData> Decorators, string Text)> BuildCommentPayloadAsync(List<BaseContent> contents, List<StickerContent> stickerContents, MediaAttachmentViewModel attachmentViewModel)
    {
        // The editor appends a '\n' after a sticker image token so it renders on its own line.
        // When a sticker is the last element, that trailing newline becomes a whitespace-only
        // text content and would be posted as an empty line after the sticker image — so strip
        // trailing whitespace-only text contents before building the payload.
        TrimTrailingWhitespaceTextContents(contents);

        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromContents(contents);

        // Stickers resolve to an uploaded image when possible; failed uploads are ignored.
        var imageQuoteDatas = new List<QuoteData>();
        foreach (var stickerContent in stickerContents)
        {
            if (stickerContent.StickerMediaId == null) continue;

            var imageData = await MentionHelper.GetStickerImageDataAsync(stickerContent.StickerMediaId);
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
            var uploadedImage = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImageProp(attachmentViewModel.FilePath));
            imageQuoteDatas.Insert(0, new QuoteData
            {
                type = "image",
                text = "(Image) ",
                media_path = BuildKakaoMediaPath(uploadedImage)
            });
        }

        var decorators = imageQuoteDatas.Concat(quoteDatas).ToList();
        // The API expects the plain text to mirror the decorators (KSMP pattern: space-joined decorator texts).
        var plainText = string.Join(' ', decorators.Select(x => x.text));
        return (decorators, plainText);
    }

    private static string BuildKakaoMediaPath(UploadedImageProp uploadedImage) => $"{uploadedImage.access_key}/{uploadedImage.info.original.filename}?width={uploadedImage.info.original.width}&height={uploadedImage.info.original.height}&avg={uploadedImage.info.original.avg}";

    private static void TrimTrailingWhitespaceTextContents(List<BaseContent> contents)
    {
        while (contents.Count > 0 && contents[^1] is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text))
            contents.RemoveAt(contents.Count - 1);
    }
}
