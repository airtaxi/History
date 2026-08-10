using System.Text;
using System.Text.RegularExpressions;
using History.Commons.Api.PushNotification;
using History.Uno.Enums;
using History.Uno.ViewModels;
using Microsoft.UI.Xaml;
using Plugin.Firebase.CloudMessaging;

namespace History.Uno;

public static partial class Utils
{
    private const int TimelineMaxTextLengthWithoutMedias = 400;
    private const int TimelineMaxTextLengthWithMedias = 80;
    private const int TimelineMaxTextLinesWithoutMedias = 12;
    private const int TimelineMaxTextLinesWithMedias = 8;
    private const int DiscoveryMaxTextLength = 1600;
    private const int DiscoveryMaxTextLines = 27;

    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }

    public static List<IContentViewModel> GenerateContentViewModels(IEnumerable<BaseContent> contents, PostType postType, bool isParentPost = false, string postId = null)
    {
        var contentViewModels = new List<IContentViewModel>();

        var mediaContents = new List<MediaContent>();
        var allMediaContents = contents.OfType<MediaContent>();
        void FlushMediaContents()
        {
            if (mediaContents.Count > 0)
            {
                contentViewModels.Add(new WrappedMediaContentsViewModel(mediaContents, allMediaContents, postType, isParentPost));
                mediaContents = [];
            }
        }

        var textTypeContents = new List<BaseContent>();
        void FlushTextTypeContents()
        {
            if (textTypeContents.Count > 0)
            {
                contentViewModels.Add(new TextTypeContentsViewModel(textTypeContents, postType, contents.OfType<MediaContent>().Any() || contents.OfType<ExternalUrlContent>().Any()));
                textTypeContents = [];
            }
        }

        // Fill contentViewModels with contents
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
                // To prevent abusing stickers in timeline or discovery
                if (postType != PostType.Unwrapped && contentViewModels.Any(x => x is StickerContentViewModel)) continue;

                FlushMediaContents();
                FlushTextTypeContents();
                contentViewModels.Add(new StickerContentViewModel(stickerContent));
            }
            else if (content is ExternalUrlContent externalUrlContent)
            {
                FlushMediaContents();
                FlushTextTypeContents();
                contentViewModels.Add(new ExternalUrlContentViewModel(externalUrlContent));
            }
            else if (content is PollContent pollContent)
            {
                FlushMediaContents();
                FlushTextTypeContents();
                contentViewModels.Add(new PollContentViewModel(pollContent, postId));
            }
            else if (content is MediaContent mediaContent)
            {
                FlushTextTypeContents();
                if (postType != PostType.Unwrapped) mediaContents.Add(mediaContent);
                else contentViewModels.Add(new MediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost));
            }
        }

        // Flush remaining contents
        FlushTextTypeContents();
        FlushMediaContents();

        return contentViewModels;
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

        var textTypeContents = contents.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent || x is HyperlinkContent);

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
        var textTypeContents = contents.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent || x is HyperlinkContent);
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

    /// <summary>
    /// Converts text/profile/hashtag contents into a list of runs with timeline truncation
    /// applied ("... 더보기" marker), ready to be rendered in a TextBlock.
    /// </summary>
    public static List<TextContentRun> GenerateTextContentRuns(List<BaseContent> contents, PostType postType, bool hasMedias)
    {
        var runs = new List<TextContentRun>();
        var maxLength = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLengthWithMedias : TimelineMaxTextLengthWithoutMedias) : DiscoveryMaxTextLength;
        var maxLines = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLinesWithMedias : TimelineMaxTextLinesWithoutMedias) : DiscoveryMaxTextLines;
        var currentLength = 0;
        var currentLines = 0;

        void AddMoreRun(TextContentRun run)
        {
            runs.Add(run);
            runs.Add(new TextContentRun(" ... 더보기", true, TextContentRunKind.Plain, colorHex: "#999999"));
        }

        void TrimRun(TextContentRun run)
        {
            if (currentLines > maxLines)
            {
                var lines = run.Text.Split(["\r\n", "\n"], StringSplitOptions.None);
                var allowedLines = maxLines - (currentLines - lines.Length);

                if (allowedLines <= 0) run.Text = string.Empty;
                else run.Text = string.Join(Environment.NewLine, lines.Take(allowedLines));
            }
            else if (currentLength > maxLength)
            {
                var allowedLength = maxLength - (currentLength - run.Text.Length);
                if (allowedLength >= 0) run.Text = run.Text[..allowedLength];
            }
        }

        void AddRun(TextContentRun run, ref bool breaked)
        {
            currentLength += run.Text.Length;
            currentLines += run.Text.Count(x => x == '\n');
            if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
            {
                TrimRun(run);
                AddMoreRun(run);
                breaked = true;
            }
            else runs.Add(run);
        }

        foreach (var content in contents)
        {
            if (content is TextContent textContent)
            {
                var matches = UrlRegex().Matches(textContent.Text);
                var lastIndex = 0;
                var breaked = false;
                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        AddRun(new TextContentRun(textContent.Text[lastIndex..match.Index], false, TextContentRunKind.Plain), ref breaked);
                        if (breaked) break;
                    }

                    if (breaked) break;

                    AddRun(new TextContentRun(match.Value, false, TextContentRunKind.Link, match.Value, "#ED664D"), ref breaked);
                    lastIndex = match.Index + match.Length;
                    if (breaked) break;
                }
                if (breaked) break;

                if (lastIndex < textContent.Text.Length)
                {
                    AddRun(new TextContentRun(textContent.Text[lastIndex..], false, TextContentRunKind.Plain), ref breaked);
                    if (breaked) break;
                }
            }
            else if (content is ProfileContent profileContent)
            {
                var breaked = false;
                AddRun(new TextContentRun(profileContent.Nickname, true, TextContentRunKind.Profile, profileContent.UserId, "#ED664D"), ref breaked);
                if (breaked) break;
            }
            else if (content is HashtagContent hashtagContent)
            {
                var breaked = false;
                AddRun(new TextContentRun($"#{hashtagContent.Tag}", true, TextContentRunKind.Hashtag, hashtagContent.Tag, "#ED664D"), ref breaked);
                if (breaked) break;
            }
            else if (content is HyperlinkContent hyperlinkContent)
            {
                var breaked = false;
                AddRun(new TextContentRun(hyperlinkContent.Url, false, TextContentRunKind.Link, hyperlinkContent.Url, "#ED664D"), ref breaked);
                if (breaked) break;
            }
        }

        return runs;
    }

    public static ApplicationTheme GetGlobalAppTheme() => Application.Current.RequestedTheme;

    public static string GetDiscoveryOptionGlyph(DiscoveryOption option)
    {
        return option switch
        {
            DiscoveryOption.OnlyMe => "\uE72E",           // Lock
            DiscoveryOption.SelectedUsers => "\uE8FA",    // AddFriend
            DiscoveryOption.UnselectedUsers => "\uE8F8",  // BlockContact
            DiscoveryOption.Friends => "\uE716",          // People
            DiscoveryOption.FriendsOfFriends => "\uEBDA", // Family
            DiscoveryOption.Everyone => "\uE774",         // Globe
            _ => "\uF142",                                // StatusCircleQuestionMark
        };
    }

    public static async Task RefreshFirebaseToken()
    {
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        var firebaseToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        Console.WriteLine($"FCM token: {firebaseToken}");

        if (Shared.ApiHandler == null)
        {
            var accessToken = Configuration.GetValue<string>("AccessToken");
            var refreshToken = Configuration.GetValue<string>("RefreshToken");

            if (accessToken != null && refreshToken != null) Shared.ApiHandler = new(accessToken, refreshToken);
            else return;
        }

        try { await Shared.ApiHandler.ExecuteRequestAsync(new RegisterFirebaseToken(firebaseToken)); }
        catch { }
    }
}