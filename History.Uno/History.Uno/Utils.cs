using System.Text;
using System.Text.RegularExpressions;

namespace History.Uno;

public static partial class Utils
{
    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
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
                var textContent = new TextContent() { Text = text };
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

        var textTypeContents = contents.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent);

        var firstContent = textTypeContents.FirstOrDefault();
        if (firstContent is TextContent firstTextContent) firstTextContent.Text = firstTextContent.Text.TrimStart();

        var lastContent = textTypeContents.LastOrDefault();
        if (lastContent is TextContent lastTextContent) lastTextContent.Text = lastTextContent.Text.TrimEnd();

        contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrEmpty(textContent.Text));
    }

    public static string GenerateFriendlyTimestamp(DateTime createdAt, DateTime? modifiedAt)
    {
        var time = DateTime.UtcNow - createdAt;
        string result;
        if (time.TotalSeconds < 60)
            result = $"방금 전";
        else if (time.TotalMinutes < 60)
            result = $"{time.TotalMinutes:N0}분 전";
        else if (time.TotalHours < 2)
            result = $"{time.TotalHours:N0}시간 전";
        else if (createdAt.Year == DateTime.UtcNow.Year) result = $"{createdAt.ToLocalTime():MM월 dd일 HH:mm}";
        else result = $"{createdAt.ToLocalTime():yyyy년 MM월dd일 HH:mm:ss}";

        if (modifiedAt != null) result += $" (수정됨)";

        return result;
    }

    public static string GenerateTextPreviewFromContents(IEnumerable<BaseContent> contents)
    {
        var textTypeContents = contents.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent);
        var builder = new StringBuilder();
        foreach (var content in textTypeContents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
            else if (content is HashtagContent hashtagContent) builder.Append($"#{hashtagContent.Tag}");
        }

        var result = builder.ToString();
        result = result.ReplaceLineEndings("\n");
        while (result.Contains("\n\n")) result = result.Replace("\n\n", "\n");
        return result;
    }

    public static string GenerateThumbnailUrlFromContents(IEnumerable<BaseContent> contents)
    {
        string imageUrl = null;

        var mediaId = contents.OfType<MediaContent>().Where(x => !x.IsSpoiler).Select(x => x.ThumbnailMediaId).FirstOrDefault()
            ?? contents.OfType<StickerContent>().Select(x => x.StickerMediaId).FirstOrDefault();
        if (mediaId == null) imageUrl = contents.OfType<ExternalUrlContent>().Select(x => x.ThumbnailImageUrl).FirstOrDefault();

        if (imageUrl == null && mediaId != null) imageUrl = GenerateMediaUri(mediaId);

        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) imageUrl = null;

        return imageUrl;
    }

    public static string GenerateThumbnailUrlFromPost(PostResponseDto post)
    {
        var imageUrl = GenerateThumbnailUrlFromContents(post.Contents);
        if (imageUrl == null && post.ParentPost != null) imageUrl = GenerateThumbnailUrlFromContents(post.ParentPost.Contents);
        return imageUrl;
    }

    public static string GenerateTextPreviewFromPost(PostResponseDto post)
    {
        var preview = GenerateTextPreviewFromContents(post.Contents);
        if (string.IsNullOrWhiteSpace(preview) && post.ParentPost != null) preview = GenerateTextPreviewFromContents(post.ParentPost.Contents);
        if (string.IsNullOrWhiteSpace(preview))
        {
            var hashtagContents = post.Contents.OfType<HashtagContent>().ToList();
            if (hashtagContents.Count > 0) preview = string.Join(" ", hashtagContents.Select(x => $"#{x.Tag}"));
        }
        return preview;
    }

    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    public static partial Regex UrlRegex();
}