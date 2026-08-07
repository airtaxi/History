using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Graphics.Platform;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.KakaoStory;

// Shared Kakao Story comment payload builder used by both comment creation (PostPage)
// and comment editing (EditCommentPage). Stickers are uploaded as images when possible
// (webp is converted to PNG since Kakao Story does not accept webp); otherwise they
// degrade to "(스티커)" text. The picker image is uploaded and placed first so the API
// renders the first decorator as the comment image.
public static class KakaoStoryCommentHelper
{
    public static async Task<(List<QuoteData> Decorators, string Text)> BuildCommentPayloadAsync(
        string text, List<StickerContent> stickerContents, MediaAttachmentViewModel attachmentViewModel)
    {
        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromString(text);

        // Stickers resolve to an uploaded image when possible; otherwise "(스티커)" stays as text.
        var imageQuoteDatas = new List<QuoteData>();
        var uploadedStickerCount = 0;
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
                uploadedStickerCount++;
            }
            finally { try { File.Delete(tempFilePath); } catch { } }
        }

        // Drop the "(스티커)" placeholders that were replaced by uploaded images.
        if (uploadedStickerCount > 0)
        {
            var remaining = new List<QuoteData>();
            var toRemove = uploadedStickerCount;
            foreach (var quoteData in quoteDatas)
            {
                if (quoteData.type == "text")
                {
                    var fragment = quoteData.text;
                    while (toRemove > 0 && fragment != null && fragment.Contains("(스티커)", StringComparison.Ordinal))
                    {
                        // Prefer removing "(스티커)\n" (sticker on its own line) so no blank line remains.
                        var index = fragment.IndexOf("(스티커)\n", StringComparison.Ordinal);
                        if (index >= 0) fragment = fragment.Remove(index, "(스티커)\n".Length);
                        else
                        {
                            index = fragment.IndexOf("(스티커)", StringComparison.Ordinal);
                            fragment = fragment.Remove(index, "(스티커)".Length);
                        }
                        toRemove--;
                    }
                    quoteData.text = fragment;
                    if (!string.IsNullOrEmpty(fragment)) remaining.Add(quoteData);
                }
                else remaining.Add(quoteData);
            }
            quoteDatas = remaining;
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

    private static string BuildKakaoMediaPath(KakaoStoryApiHandler.DataType.UploadedImageProp uploadedImage) =>
        $"{uploadedImage.access_key}/{uploadedImage.info.original.filename}?width={uploadedImage.info.original.width}&height={uploadedImage.info.original.height}&avg={uploadedImage.info.original.avg}";
}
