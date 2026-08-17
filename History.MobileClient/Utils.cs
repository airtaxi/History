using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Alerts;
using History.Commons;
using History.Commons.Api.PushNotification;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Plugin.Firebase.CloudMessaging;
using UraniumUI.Icons.FontAwesome;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.Maui.Graphics.Platform;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Messages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;


namespace History.MobileClient;

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
                contentViewModels.Add(new HistoryWrappedMediaContentsViewModel(mediaContents, allMediaContents, postType, isParentPost));
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
#if ANDROID
                mediaContents.Add(mediaContent);
#else
                if (postType != PostType.Unwrapped) mediaContents.Add(mediaContent);
                else contentViewModels.Add(new HistoryMediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost));
#endif
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

    /// <summary>
    /// Resizes image bytes into a thumbnail that fits within the given size box
    /// (aspect ratio preserved) using the built-in MAUI image converter. Falls back
    /// to the original bytes when the conversion fails (e.g. GIF/webp animation).
    /// </summary>
    public static Task<byte[]> ResizeImageToThumbnailAsync(byte[] imageBytes, int maxSize = 256)
    {
        return Task.Run(async () =>
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);
                using var image = PlatformImage.FromStream(stream);
                if (image == null) return imageBytes;

                using var resized = image.Downsize(maxSize, maxSize);
                using var output = new MemoryStream();
                await resized.SaveAsync(output, ImageFormat.Png).ConfigureAwait(false);
                return output.ToArray();
            }
            catch { return imageBytes; }
        });
    }

    /// <summary>
    /// Resizes an image file into a thumbnail that fits within the given size box
    /// (aspect ratio preserved) using the built-in MAUI image converter. Falls back
    /// to the original file bytes when the conversion fails (e.g. GIF/webp animation),
    /// or returns null when the file is no longer readable.
    /// </summary>
    public static Task<byte[]> ResizeImageToThumbnailAsync(string filePath, int maxSize = 256)
    {
        return Task.Run(async () =>
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var image = PlatformImage.FromStream(stream);
                if (image == null) return await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);

                using var resized = image.Downsize(maxSize, maxSize);
                using var output = new MemoryStream();
                await resized.SaveAsync(output, ImageFormat.Png).ConfigureAwait(false);
                return output.ToArray();
            }
            catch
            {
                try { return await File.ReadAllBytesAsync(filePath).ConfigureAwait(false); }
                catch { return null; }
            }
        });
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

    public static FormattedString GenerateFormattedStringFromTextTypeContents(List<BaseContent> contents, PostType postType, bool hasMedias)
    {
        var formattedString = new FormattedString();
        var maxLength = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLengthWithMedias : TimelineMaxTextLengthWithoutMedias) : DiscoveryMaxTextLength;
        var maxLines = postType == PostType.Timeline ? (hasMedias ? TimelineMaxTextLinesWithMedias : TimelineMaxTextLinesWithoutMedias) : DiscoveryMaxTextLines;
        var currentLength = 0;
        var currentLines = 0;

        foreach (var content in contents)
        {
            void AddMoreSpan(Span span)
            {
                formattedString.Spans.Add(span);
                formattedString.Spans.Add(new Span
                {
                    Text = " ... 더보기",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromRgb(0x99, 0x99, 0x99)
                });
            }

            void TrimSpan(Span span)
            {
                if (currentLines > maxLines)
                {
                    var lines = span.Text.Split(["\r\n", "\n"], StringSplitOptions.None);
                    var allowedLines = maxLines - (currentLines - lines.Length);

                    if (allowedLines <= 0) span.Text = string.Empty;
                    else span.Text = string.Join(Environment.NewLine, lines.Take(allowedLines));
                }
                else if (currentLength > maxLength)
                {
                    var allowedLength = maxLength - (currentLength - span.Text.Length);
                    if (allowedLength >= 0) span.Text = span.Text[..allowedLength];
                }
            }

            if (content is TextContent textContent)
            {
                var matches = UrlRegex().Matches(textContent.Text);
                int lastIndex = 0;
                var breaked = false;
                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        string plainText = textContent.Text[lastIndex..match.Index];

                        var span = new Span { Text = plainText };

                        currentLength += span.Text.Length;
                        currentLines += span.Text.Count(x => x == '\n');
                        if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                        {
                            TrimSpan(span);
                            AddMoreSpan(span);
                            breaked = true;
                            break;
                        }
                        else formattedString.Spans.Add(span);
                    }

                    string url = match.Value;

                    var linkSpan = new Span
                    {
                        Text = url,
                        TextColor = Application.Current.Resources["Primary"] as Color
                    };
                    AddTapGestureRecognizerToLinkSpan(linkSpan, url);

                    lastIndex = match.Index + match.Length;

                    currentLength += linkSpan.Text.Length;
                    currentLines += linkSpan.Text.Count(x => x == '\n');
                    if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                    {
                        TrimSpan(linkSpan);
                        AddMoreSpan(linkSpan);
                        breaked = true;
                        break;
                    }
                    else formattedString.Spans.Add(linkSpan);
                }
                if (breaked) break;

                if (lastIndex < textContent.Text.Length)
                {
                    string remaining = textContent.Text[lastIndex..];

                    var span = new Span { Text = remaining };

                    currentLength += span.Text.Length;
                    currentLines += span.Text.Count(x => x == '\n');
                    if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                    {
                        TrimSpan(span);
                        AddMoreSpan(span);
                        break;
                    }
                    else formattedString.Spans.Add(span);
                }
            }
            else if (content is ProfileContent profileContent)
            {
                var span = new Span
                {
                    Text = profileContent.Nickname,
                    TextColor = Application.Current.Resources["Primary"] as Color,
                    FontAttributes = FontAttributes.Bold,
                };

                if (profileContent.UserId != null) AddTapGestureRecognizerToProfileContentSnap(span, profileContent.UserId);

                currentLength += span.Text.Length;
                currentLines += span.Text.Count(x => x == '\n');
                if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                {
                    TrimSpan(span);
                    AddMoreSpan(span);
                    break;
                }
                else formattedString.Spans.Add(span);
            }
            else if (content is HashtagContent hashtagContent)
            {
                var span = new Span
                {
                    Text = $"#{hashtagContent.Tag}",
                    TextColor = Application.Current.Resources["Primary"] as Color,
                    FontAttributes = FontAttributes.Bold,
                };

                AddTapGestureRecognizerToHashtagSpan(span, hashtagContent.Tag);

                currentLength += span.Text.Length;
                currentLines += span.Text.Count(x => x == '\n');
                if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                {
                    TrimSpan(span);
                    AddMoreSpan(span);
                    break;
                }
                else formattedString.Spans.Add(span);
            }
            else if (content is HyperlinkContent hyperlinkContent)
            {
                var span = new Span
                {
                    Text = hyperlinkContent.Url,
                    TextColor = Application.Current.Resources["Primary"] as Color,
                };

                AddTapGestureRecognizerToLinkSpan(span, hyperlinkContent.Url);

                currentLength += span.Text.Length;
                currentLines += span.Text.Count(x => x == '\n');
                if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                {
                    TrimSpan(span);
                    AddMoreSpan(span);
                    break;
                }
                else formattedString.Spans.Add(span);
            }
        }

        return formattedString;
    }

    // Kakao Story variant: renders QuoteData (text/hashtag/profile/emoticon) into a FormattedString.
    // URLs inside text decorators are rendered as tappable links, matching the History path.
    public static FormattedString GenerateFormattedStringFromQuoteData(List<QuoteData> quoteDatas, PostType postType)
    {
        var formattedString = new FormattedString();
        var maxLength = postType == PostType.Timeline ? 400 : 1600;
        var maxLines = postType == PostType.Timeline ? 12 : 27;
        var currentLength = 0;
        var currentLines = 0;

        foreach (var data in quoteDatas)
        {
            if (data.type == "image" || data.type == "emoticon") continue;

            void AddMoreSpan(Span span)
            {
                formattedString.Spans.Add(span);
                formattedString.Spans.Add(new Span
                {
                    Text = " ... 더보기",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromRgb(0x99, 0x99, 0x99)
                });
            }

            void TrimSpan(Span span)
            {
                if (currentLines > maxLines)
                {
                    var lines = span.Text.Split(["\r\n", "\n"], StringSplitOptions.None);
                    var allowedLines = maxLines - (currentLines - lines.Length);
                    if (allowedLines <= 0) span.Text = string.Empty;
                    else span.Text = string.Join(Environment.NewLine, lines.Take(allowedLines));
                }
                else if (currentLength > maxLength)
                {
                    var allowedLength = maxLength - (currentLength - span.Text.Length);
                    if (allowedLength >= 0) span.Text = span.Text[..allowedLength];
                }
            }

            // Adds a span with trimming applied; returns false when the text limit is reached.
            bool TryAddSpan(Span span)
            {
                currentLength += span.Text.Length;
                currentLines += span.Text.Count(x => x == '\n');
                if (postType != PostType.Unwrapped && (currentLength > maxLength || currentLines > maxLines))
                {
                    TrimSpan(span);
                    AddMoreSpan(span);
                    return false;
                }
                formattedString.Spans.Add(span);
                return true;
            }

            if (data.type == "text")
            {
                var text = data.text ?? string.Empty;
                var matches = UrlRegex().Matches(text);
                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex && !TryAddSpan(new Span { Text = text[lastIndex..match.Index] })) return formattedString;

                    var url = match.Value;
                    var linkSpan = new Span
                    {
                        Text = url,
                        TextColor = Application.Current.Resources["Primary"] as Color,
                    };
                    AddTapGestureRecognizerToLinkSpan(linkSpan, url);
                    lastIndex = match.Index + match.Length;
                    if (!TryAddSpan(linkSpan)) return formattedString;
                }

                if (lastIndex < text.Length && !TryAddSpan(new Span { Text = text[lastIndex..] })) return formattedString;
            }
            else
            {
                var span = new Span
                {
                    Text = data.text,
                    TextColor = data.type is "hashtag" or "profile" ? Application.Current.Resources["Primary"] as Color : null,
                    FontAttributes = data.type is "hashtag" or "profile" ? FontAttributes.Bold : FontAttributes.None
                };

                // Kakao Story @-mention: open the mentioned user's Kakao Story profile on tap.
                if (data.type == "profile" && data.id != null) AddTapGestureRecognizerToKakaoProfileSpan(span, data.id);

                if (!TryAddSpan(span)) return formattedString;
            }
        }

        return formattedString;
    }

    public static AppTheme GetGlobalAppTheme()
    {
        var theme = Application.Current.UserAppTheme;
        if (theme == AppTheme.Unspecified) theme = Application.Current.PlatformAppTheme;
        else if (theme == AppTheme.Light) theme = AppTheme.Light;
        else if (theme == AppTheme.Dark) theme = AppTheme.Dark;
        return theme;
    }

    // Returns the Korean subject particle (이/가) for the given word.
    public static string GetSubjectParticle(string word) => HasJongsung(word) ? "이" : "가";

    // Returns the Korean topic particle (은/는) for the given word.
    public static string GetTopicParticle(string word) => HasJongsung(word) ? "은" : "는";

    // Returns the Korean object particle (을/를) for the given word.
    public static string GetObjectParticle(string word) => HasJongsung(word) ? "을" : "를";

    private static bool HasJongsung(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;

        var lastCharacter = word[^1];
        if (!KoreanHelper.IsKoreanCharacrer(lastCharacter)) return false;

        var split = KoreanHelper.SplitCharacter(lastCharacter);
        return split.Length == 3 && split[2] != ' ';
    }

    private static void AddTapGestureRecognizerToLinkSpan(Span linkSpan, string url)
    {
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) => await OpenLinkAsync(url);

        linkSpan.GestureRecognizers.Add(tapGesture);
    }

    // Opens a link. Kakao Story post URLs (https://story.kakao.com/{username}/{postCode})
    // navigate to the in-app post page instead of the external browser. The post id is
    // resolved by fetching the page and extracting the embedded feed_id.
    public static async Task OpenLinkAsync(string url)
    {
        var postId = await GetKakaoStoryPostIdAsync(url);
        if (postId != null)
        {
            await OpenKakaoStoryPostAsync(postId);
            return;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host == "story.kakao.com")
        {
            await App.TopPage.DisplayAlertAsync("오류", "카카오스토리 게시글을 불러오지 못했습니다.", Constants.PromptOk);
            return;
        }

        await Launcher.Default.OpenAsync(url);
    }

    // Resolves the real post id of a story.kakao.com post URL. The URL only carries
    // a short code (e.g. eNOIUHoOHQA), so the authenticated page must be fetched to
    // read the embedded feed_id (e.g. "_63msr.6MgT2Z7CfP9"). Returns false when the
    // URL is not a story post URL or the id cannot be resolved.
    private static async Task<string> GetKakaoStoryPostIdAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Host != "story.kakao.com") return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2) return null;

        if ((await KakaoStoryUtils.EnsureLoggedInAsync(App.TopPage)) == false) return null;

        var page = await KakaoStoryApiHandler.GetPostPageAsync(url);
        if (page == null) return null;

        var match = KakaoStoryFeedIdRegex().Match(page);
        if (!match.Success) return null;

        return match.Groups[1].Value;
    }

    private static async Task OpenKakaoStoryPostAsync(string postId)
    {
        if ((await KakaoStoryUtils.EnsureLoggedInAsync(App.TopPage)) == false) return;

        WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));
        try
        {
            KakaoPostViewModel postViewModel = null;
            await Task.Run(async () =>
            {
                var post = await KakaoStoryApiHandler.GetPost(postId);
                if (post == null) return;

                postViewModel = new KakaoPostViewModel(post, PostType.Unwrapped);
            });

            if(postViewModel == null) return;

            await App.PushAsync(new PostPage(postViewModel));
        }
        catch (Exception exception) { Debug.WriteLine($"Kakao Story post link navigation failed: {exception.Message}"); }
        finally { WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(false)); }
    }

    private static void AddTapGestureRecognizerToProfileContentSnap(Span span, string userId)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushAsync(new BlazorUserPage(userId));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }

    private static void AddTapGestureRecognizerToKakaoProfileSpan(Span span, string kakaoUserId)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushAsync(new BlazorUserPage(kakaoUserId, true));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }

    private static void AddTapGestureRecognizerToHashtagSpan(Span span, string hashtag)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushAsync(new EditPostPage([hashtag]));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }

    public static async Task RefreshFirebaseToken()
    {
        try
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

            await Shared.ApiHandler.ExecuteRequestAsync(new RegisterFirebaseToken(firebaseToken));
        }
        catch { }
    }

    public static string GetDiscoveryOptionGlyph(DiscoveryOption option)
    {
        return option switch
        {
            DiscoveryOption.OnlyMe => Solid.Lock,
            DiscoveryOption.SelectedUsers => Solid.UserPlus,
            DiscoveryOption.UnselectedUsers => Solid.UserMinus,
            DiscoveryOption.Friends => Solid.Users,
            DiscoveryOption.FriendsOfFriends => Solid.UsersBetweenLines,
            DiscoveryOption.Everyone => Solid.Globe,
            _ => Solid.Question
        };
    }

    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    public static partial Regex UrlRegex();

    // Captures the feed_id from a story.kakao.com post page response. The feed_id
    // is the real post id used by the story API (e.g. "_63msr.6MgT2Z7CfP9").
    [GeneratedRegex("\"feed_id\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled)]
    private static partial Regex KakaoStoryFeedIdRegex();

    public static async Task CheckForUpdateAsync()
    {
        try
        {
#if ANDROID
            var versionUrl = "https://kagamine-rin.com/History/version_android";
#else
            var versionUrl = "https://kagamine-rin.com/History/version_ios";
#endif
            var remoteVersionString = await Downloader.DownloadString(versionUrl);
            var localVersionString = AppInfo.Current.VersionString;

            var remoteVersion = Version.Parse(remoteVersionString);
            var localVersion = Version.Parse(localVersionString);
            if (remoteVersion <= localVersion)
            {
                await Toast.Make("최신 버전을 사용중입니다.").Show();
                return;
            }
#if ANDROID
            var update = await App.Page.DisplayAlertAsync("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})\nGoogle Play에서 업데이트해 주세요.", Constants.PromptOk, Constants.PromptCancel);
            if (update) await Launcher.Default.OpenAsync("market://details?id=com.airtaxi.history");
#else
            var update = await App.Page.DisplayAlertAsync("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})\nTestFlight에서 업데이트해 주세요.", Constants.PromptOk, Constants.PromptCancel);
            if (update) await Launcher.Default.OpenAsync("https://testflight.apple.com/join/WpphZnwe");
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"History Update Error: {ex.Message}");
            await App.Page.DisplayAlertAsync("오류", $"업데이트 중 문제가 발생했습니다: {ex.Message}", "확인");
        }
    }
}
