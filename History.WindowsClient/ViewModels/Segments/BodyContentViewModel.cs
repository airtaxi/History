using System.Text.RegularExpressions;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels.Segments;

// Converts a List<BaseContent> into renderable segments, decomposing the text
// content into text/profile/hashtag/hyperlink segments. The navigation host is
// threaded into profile segments so nickname taps can request profile navigation.
// MediaContent, ExternalUrlContent, PollContent and UploadContent are not body
// contents and are skipped; they are rendered by separate surfaces.
// Long text is truncated to the configured length/line limits with a trailing
// " ... 더보기" marker; Unwrapped posts render their full text.
public partial class BodyContentViewModel : BaseViewModel
{
    // Text truncation limits per post type and media presence.
    private const int TimelineMaxTextLengthWithoutMedias = 400;
    private const int TimelineMaxTextLengthWithMedias = 80;
    private const int TimelineMaxTextLinesWithoutMedias = 12;
    private const int TimelineMaxTextLinesWithMedias = 8;
    private const int DiscoveryMaxTextLength = 1600;
    private const int DiscoveryMaxTextLines = 27;

    public List<BodyContentSegmentViewModel> Segments { get; private set; } = [];

    public void Update(List<BaseContent> contents, BaseViewModel baseViewModel, PostType postType, bool hasMedias)
    {
        var segments = new List<BodyContentSegmentViewModel>();
        var maxTextLength = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLengthWithMedias : TimelineMaxTextLengthWithoutMedias) : DiscoveryMaxTextLength;
        var maxTextLines = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLinesWithMedias : TimelineMaxTextLinesWithoutMedias) : DiscoveryMaxTextLines;
        var currentLength = 0;
        var currentLines = 0;

        // Appends the " ... 더보기" marker at the truncation point.
        void AddMoreSegment() => segments.Add(new MoreSegmentViewModel());

        // Trims the overrunning span text to fit within the configured limits.
        string TrimText(string text)
        {
            if (currentLines > maxTextLines)
            {
                var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
                var allowedLines = maxTextLines - (currentLines - lines.Length);
                return allowedLines <= 0 ? string.Empty : string.Join(Environment.NewLine, lines.Take(allowedLines));
            }
            if (currentLength > maxTextLength)
            {
                var allowedLength = maxTextLength - (currentLength - text.Length);
                return allowedLength >= 0 ? text[..allowedLength] : string.Empty;
            }
            return text;
        }

        // Adds a text-based segment with limit checking; returns the display text when
        // the limit was reached (the marker is appended) or null when appended normally.
        string TryAddTextSegment(string text, Func<string, BodyContentSegmentViewModel> createSegment)
        {
            currentLength += text.Length;
            currentLines += text.Count(character => character == '\n');
            if (postType != PostType.Unwrapped && (currentLength > maxTextLength || currentLines > maxTextLines))
            {
                var trimmedText = TrimText(text);
                segments.Add(createSegment(trimmedText));
                AddMoreSegment();
                return trimmedText;
            }
            segments.Add(createSegment(text));
            return null;
        }

        // Splits text into plain text and URL segments so URLs inside plain text become
        // tappable links; returns false when the text limit was reached.
        bool AppendTextSegments(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            var lastIndex = 0;
            foreach (Match match in UrlRegex().Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    var plainText = text[lastIndex..match.Index];
                    if (TryAddTextSegment(plainText, static text => new TextSegmentViewModel(text)) != null) return false;
                }

                var url = match.Value;
                if (TryAddTextSegment(url, trimmedText => new UrlSegmentViewModel(url, trimmedText)) != null) return false;
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                var remaining = text[lastIndex..];
                if (TryAddTextSegment(remaining, static text => new TextSegmentViewModel(text)) != null) return false;
            }
            return true;
        }

        if (contents != null)
        {
            foreach (var content in contents)
            {
                if (content is TextContent textContent && !AppendTextSegments(textContent.Text)) break;
                else if (content is ProfileContent profileContent && TryAddTextSegment(profileContent.Nickname, text => new ProfileSegmentViewModel(profileContent.UserId, text, baseViewModel)) != null) break;
                else if (content is HashtagContent hashtagContent && TryAddTextSegment($"#{hashtagContent.Tag}", text => new HashtagSegmentViewModel(text[1..])) != null) break;
                else if (content is HyperlinkContent hyperlinkContent && TryAddTextSegment(hyperlinkContent.Url, trimmedText => new HyperlinkSegmentViewModel(hyperlinkContent.Url, trimmedText)) != null) break;
                else if (content is StickerContent stickerContent) segments.Add(new StickerSegmentViewModel(stickerContent));
            }
        }
        Segments = segments;
    }

    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    private static partial Regex UrlRegex();
}