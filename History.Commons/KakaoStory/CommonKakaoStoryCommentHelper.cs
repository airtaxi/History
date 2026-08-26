using History.Commons.DataTypes.Contents;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.Commons.KakaoStory;

public partial class CommonKakaoStoryCommentHelper
{
    protected static string BuildKakaoMediaPath(UploadedImageProp uploadedImage) => $"{uploadedImage.access_key}/{uploadedImage.info.original.filename}?width={uploadedImage.info.original.width}&height={uploadedImage.info.original.height}&avg={uploadedImage.info.original.avg}";

    protected static void TrimTrailingWhitespaceTextContents(List<BaseContent> contents)
    {
        while (contents.Count > 0 && contents[^1] is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text))
            contents.RemoveAt(contents.Count - 1);
    }

    protected static void TrimNewlinesAfterStickers(List<BaseContent> contents)
    {
        for (int index = 0; index < contents.Count; index++)
        {
            if (contents[index] is not StickerContent) continue;

            // Strip the leading newlines from the text that follows the sticker
            // (the '\n' the editor appends after a sticker token plus any user-typed ones).
            if (index + 1 < contents.Count && contents[index + 1] is TextContent bodyContent)
            {
                bodyContent.Text = bodyContent.Text.TrimStart('\n');
                if (string.IsNullOrWhiteSpace(bodyContent.Text)) contents.RemoveAt(index + 1);
            }
        }
    }
}
