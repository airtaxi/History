using System.Text.RegularExpressions;
using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes.Contents;

namespace History.ApiService;

public static partial class Utils
{
    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }

    public static string GenerateThumbnailUrlFromContents(IEnumerable<BaseContent> contents)
    {
        string imageUrl = null;
        var mediaId = contents.OfType<MediaContent>().Select(x => x.ThumbnailMediaId).FirstOrDefault();
        if (mediaId != null) imageUrl = GenerateMediaUri(mediaId);

        return imageUrl;
    }

    public static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return SanitizeRegex().Replace(text, string.Empty);
    }

    public static void SanitizeContents(List<BaseContent> contents)
    {
        contents.RemoveAll(x => x is null);
        contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrEmpty(textContent.Text));

        var cloned = contents.ToList();
        contents.Clear();
        var textContentBuffer = new List<TextContent>();

        void FlushTextContentBuffer()
        {
            if (textContentBuffer.Count > 1)
            {
                var texts = textContentBuffer.SelectMany(x => x.Text);
                var text = string.Concat(texts);
                var textContent = new TextContent() { Text = SanitizeText(text) };
                contents.Add(textContent);
            }
            else if (textContentBuffer.Count == 1) contents.Add(textContentBuffer.First());
            textContentBuffer.Clear();
        }

        foreach (var content in cloned)
        {
            if (content is TextContent textContent) textContentBuffer.Add(textContent);
            else
            {
                FlushTextContentBuffer();
                contents.Add(content);
            }
        }
        FlushTextContentBuffer();

        var textAndProfileContent = contents.Where(x => x is TextContent || x is ProfileContent);

        var firstContent = textAndProfileContent.FirstOrDefault();
        if (firstContent is TextContent firstTextContent) firstTextContent.Text = firstTextContent.Text.TrimStart();

        var lastContent = textAndProfileContent.LastOrDefault();
        if (lastContent is TextContent lastTextContent) lastTextContent.Text = lastTextContent.Text.TrimEnd();

        contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrEmpty(textContent.Text));
    }

    [GeneratedRegex(@"[\u0000-\u001F\u007F\u0080-\u009F\u202A-\u202E\u2066-\u2069\u200B-\u200D\u00A0\u202F\u180E]")]
    private static partial Regex SanitizeRegex();
}
