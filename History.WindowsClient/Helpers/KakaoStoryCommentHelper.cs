using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.KakaoStory;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.Helpers;

// Kakao Story comment payload builder: stickers and the picker image become image
// decorators when they can be uploaded. webp/heic/heif/avif are converted to PNG since
// Kakao Story does not accept them; a failed sticker conversion is skipped, while a
// failed picker-image conversion returns null so the caller aborts the comment. The
// picker image goes first so the API renders the first decorator as the comment image.
public partial class KakaoStoryCommentHelper : CommonKakaoStoryCommentHelper
{
    public static async Task<(List<QuoteData> Decorators, string Text)?> BuildCommentPayloadAsync(List<BaseContent> contents, List<StickerContent> stickerContents, byte[] attachmentData, string attachmentFileName)
    {
        // The editor appends a '\n' after a sticker image token so it renders on its own line.
        // Stickers become image decorators, so that newline must not leak into the following
        // text decorator (sticker + body would post "\nbody" with an empty first line) — strip
        // the leading newlines from the text that follows each sticker, and strip trailing
        // whitespace-only text contents (sticker alone) before building the payload.
        TrimNewlinesAfterStickers(contents);
        TrimTrailingWhitespaceTextContents(contents);

        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromContents(contents);

        // Stickers resolve to an uploaded image when possible; failed conversions are skipped.
        var imageQuoteDatas = new List<QuoteData>();
        foreach (var stickerContent in stickerContents)
        {
            if (stickerContent.StickerMediaId == null) continue;

            var imageData = await CommonUtils.GetStickerImageDataAsync(stickerContent.StickerMediaId);
            if (imageData.Length == 0) continue;

            // History stickers are webp, which Kakao Story does not accept.
            // Convert to PNG before uploading, same as the post upload flow.
            var convertedData = ImageConversionHelper.ConvertToPng(imageData);
            if (convertedData == null) continue;

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"comment_sticker_{Guid.NewGuid():N}.png");
            try
            {
                await File.WriteAllBytesAsync(tempFilePath, convertedData);
                var mediaPath = await KakaoStoryApiHandler.UploadImage(tempFilePath);
                imageQuoteDatas.Add(new QuoteData
                {
                    type = "image",
                    text = "(Image) ",
                    media_path = mediaPath
                });
            }
            finally { TryDeleteTempFile(tempFilePath); }
        }

        // The picker image goes first; the API renders the first decorator as the comment image.
        if (attachmentData is { Length: > 0 })
        {
            var needsConversion = attachmentFileName != null && KakaoStoryUtils.IsKakaoStoryUnsupportedImageFormat(attachmentFileName);
            var convertedData = needsConversion ? ImageConversionHelper.ConvertToPng(attachmentData) : attachmentData;
            if (convertedData == null) return null; // Conversion failed; abort the comment.

            // Converted images are always PNG; the original keeps its extension, and an
            // unknown filename defaults to PNG so the uploaded file always carries one.
            var extension = needsConversion || string.IsNullOrEmpty(attachmentFileName) ? ".png" : Path.GetExtension(attachmentFileName);
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"comment_attachment_{Guid.NewGuid():N}{extension}");
            try
            {
                await File.WriteAllBytesAsync(tempFilePath, convertedData);
                var mediaPath = await KakaoStoryApiHandler.UploadImage(tempFilePath);
                imageQuoteDatas.Insert(0, new QuoteData
                {
                    type = "image",
                    text = "(Image) ",
                    media_path = mediaPath
                });
            }
            finally { TryDeleteTempFile(tempFilePath); }
        }

        var decorators = imageQuoteDatas.Concat(quoteDatas).ToList();
        // The API expects the plain text to mirror the decorators (space-joined decorator texts).
        var plainText = string.Join(' ', decorators.Select(x => x.text));
        return (decorators, plainText);
    }

    private static void TryDeleteTempFile(string filePath)
    {
        try { File.Delete(filePath); }
        catch { }
    }
}
