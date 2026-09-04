using System.Text.RegularExpressions;
using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels.Segments;

// Converts a List<BaseContent> into renderable segments, decomposing the text
// content into text/profile/hashtag/hyperlink segments. The navigation host is
// threaded into profile segments so nickname taps can request profile navigation.
// MediaContent, ExternalUrlContent, PollContent and UploadContent are not body
// contents and are skipped; they are rendered by separate surfaces.
public partial class BodyContentViewModel : BaseViewModel
{
    public List<BodyContentSegmentViewModel> Segments { get; private set; } = [];

    public void Update(List<BaseContent> contents, BaseViewModel baseViewModel)
    {
        var segments = new List<BodyContentSegmentViewModel>();
        if (contents != null)
        {
            foreach (var content in contents)
            {
                if (content is TextContent textContent) AppendTextSegments(segments, textContent.Text);
                else if (content is ProfileContent profileContent) segments.Add(new ProfileSegmentViewModel(profileContent.UserId, profileContent.Nickname, baseViewModel));
                else if (content is HashtagContent hashtagContent) segments.Add(new HashtagSegmentViewModel(hashtagContent.Tag));
                else if (content is HyperlinkContent hyperlinkContent) segments.Add(new HyperlinkSegmentViewModel(hyperlinkContent.Url));
                else if (content is StickerContent stickerContent) segments.Add(new StickerSegmentViewModel(stickerContent));
            }
        }
        Segments = segments;
    }

    // Splits text into plain text and URL segments so URLs inside plain text become tappable links.
    private static void AppendTextSegments(List<BodyContentSegmentViewModel> segments, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var lastIndex = 0;
        foreach (Match match in UrlRegex().Matches(text))
        {
            if (match.Index > lastIndex) segments.Add(new TextSegmentViewModel(text[lastIndex..match.Index]));
            segments.Add(new UrlSegmentViewModel(match.Value));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length) segments.Add(new TextSegmentViewModel(text[lastIndex..]));
    }

    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    private static partial Regex UrlRegex();
}