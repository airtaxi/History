using System.Text;
using System.Text.RegularExpressions;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.ViewModels;

namespace History.WindowsClient.Helpers;

// Content/timestamp helpers ported from the MAUI client's Utils for the post templates.
public static partial class PostHelper
{
    public static string GenerateMediaUri(string mediaId) => CommonUtils.GenerateMediaUri(mediaId);

    // Fills content items with the same ordering rules as the MAUI client:
    // consecutive media contents are batched, consecutive text-type contents are
    // batched, and stickers/external URLs/polls flush both batches.
    public static List<IContentViewModel> GenerateContentViewModels(IEnumerable<BaseContent> contents, PostType postType, bool isParentPost = false, string postId = null)
    {
        var contentViewModels = new List<IContentViewModel>();

        var mediaContents = new List<MediaContent>();
        var allMediaContents = contents?.OfType<MediaContent>().ToList() ?? [];
        void FlushMediaContents()
        {
            if (mediaContents.Count > 0)
            {
                contentViewModels.Add(new WrappedMediaContentItemViewModel([.. mediaContents], allMediaContents, postType, isParentPost));
                mediaContents = [];
            }
        }

        var textTypeContents = new List<BaseContent>();
        void FlushTextTypeContents()
        {
            if (textTypeContents.Count > 0)
            {
                contentViewModels.Add(new BodyContentItemViewModel([.. textTypeContents], postType, allMediaContents.Count > 0 || contents?.OfType<ExternalUrlContent>().Any() == true, isParentPost));
                textTypeContents = [];
            }
        }

        if (contents != null)
        {
            foreach (var content in contents)
            {
                if (content is TextContent or ProfileContent or HashtagContent or HyperlinkContent)
                {
                    FlushMediaContents();
                    textTypeContents.Add(content);
                }
                else if (content is StickerContent stickerContent)
                {
                    // Prevent multiple stickers in a single post for non-unwrapped posts
                    // to prevent abusing stickers in timeline or discovery.
                    if (postType != PostType.Unwrapped && contentViewModels.Any(x => x is StickerContentItemViewModel)) continue;

                    FlushMediaContents();
                    FlushTextTypeContents();
                    contentViewModels.Add(new StickerContentItemViewModel(stickerContent));
                }
                else if (content is ExternalUrlContent externalUrlContent)
                {
                    FlushMediaContents();
                    FlushTextTypeContents();
                    contentViewModels.Add(new ExternalUrlContentItemViewModel(externalUrlContent));
                }
                else if (content is PollContent pollContent)
                {
                    FlushMediaContents();
                    FlushTextTypeContents();
                    contentViewModels.Add(new PollContentItemViewModel(pollContent, postId));
                }
                else if (content is MediaContent mediaContent)
                {
                    FlushTextTypeContents();
                    mediaContents.Add(mediaContent);
                }
            }
        }

        // Flush remaining contents
        FlushTextTypeContents();
        FlushMediaContents();

        return contentViewModels;
    }

    public static string GenerateFriendlyTimestamp(DateTime createdAt, DateTime? modifiedAt)
    {
        var time = DateTime.UtcNow - createdAt;
        string result;
        if (time.TotalSeconds < 60) result = "방금 전";
        else if (time.TotalMinutes < 60) result = $"{time.TotalMinutes:N0}분 전";
        else if (time.TotalHours < 2) result = $"{time.TotalHours:N0}시간 전";
        else if (createdAt.Year == DateTime.UtcNow.Year) result = $"{createdAt.ToLocalTime():MM월 dd일 HH:mm}";
        else result = $"{createdAt.ToLocalTime():yyyy년 MM월dd일 HH:mm:ss}";

        if (modifiedAt != null) result += " (수정됨)";

        return result;
    }

    public static string GenerateTextPreviewFromContents(IEnumerable<BaseContent> contents)
    {
        var textTypeContents = contents?.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent || x is HyperlinkContent) ?? [];
        var builder = new StringBuilder();
        foreach (var content in textTypeContents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
            else if (content is HashtagContent hashtagContent) builder.Append($"#{hashtagContent.Tag}");
            else if (content is HyperlinkContent hyperlinkContent) builder.Append(hyperlinkContent.Url);
        }

        var result = builder.ToString();
        result = result.ReplaceLineEndings("\n");
        while (result.Contains("\n\n")) result = result.Replace("\n\n", "\n");
        return result;
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

    public static string GenerateThumbnailUrlFromContents(IEnumerable<BaseContent> contents)
    {
        string imageUrl = null;

        var mediaId = contents?.OfType<MediaContent>().Where(x => !x.IsSpoiler).Select(x => x.ThumbnailMediaId).FirstOrDefault() ?? contents?.OfType<StickerContent>().Select(x => x.StickerMediaId).FirstOrDefault();
        if (mediaId == null) imageUrl = contents?.OfType<ExternalUrlContent>().Select(x => x.ThumbnailImageUrl).FirstOrDefault();

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

    // Segoe Fluent glyph mapping provided by the project owner (MAUI uses FontAwesome equivalents).
    public static string GetDiscoveryOptionGlyph(DiscoveryOption option) => option switch
    {
        DiscoveryOption.OnlyMe => "\uE72E",
        DiscoveryOption.Friends => "\uE716",
        DiscoveryOption.FriendsOfFriends => "\uE902",
        DiscoveryOption.SelectedUsers => "\uE8FA",
        DiscoveryOption.UnselectedUsers => "\uF69B",
        DiscoveryOption.Everyone => "\uE774",
        _ => "\uE9CE",
    };

    // Same pattern as the MAUI client's Utils.UrlRegex.
    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    public static partial Regex UrlRegex();
}