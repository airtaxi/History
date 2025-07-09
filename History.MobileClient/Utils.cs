using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Alerts;
using History.Commons;
using History.Commons.Api.PushNotification;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Plugin.Firebase.CloudMessaging;
using UraniumUI.Icons.FontAwesome;
using History.Commons.DataTypes.ResponseDtos;


#if ANDROID
using Android.Content;
#endif

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

    public static List<IContentViewModel> GenerateContentViewModels(IEnumerable<BaseContent> contents, PostType postType, bool isParentPost = false)
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

        var textAndProfileContents = new List<BaseContent>();
        void FlushTextAndProfileContents()
        {
            if (textAndProfileContents.Count > 0)
            {
                contentViewModels.Add(new TextAndProfileContentsViewModel(textAndProfileContents, postType, contents.OfType<MediaContent>().Any() || contents.OfType<ExternalUrlContent>().Any()));
                textAndProfileContents = [];
            }
        }

        // Fill contentViewModels with contents
        foreach (var content in contents)
        {
            if (content is TextContent or ProfileContent)
            {
                FlushMediaContents();
                textAndProfileContents.Add(content);
            }
            else if (content is StickerContent stickerContent)
            {
                FlushMediaContents();
                FlushTextAndProfileContents();
                contentViewModels.Add(new StickerContentViewModel(stickerContent));
            }
            else if (content is ExternalUrlContent externalUrlContent)
            {
                FlushMediaContents();
                FlushTextAndProfileContents();
                contentViewModels.Add(new ExternalUrlContentViewModel(externalUrlContent));
            }
            else if (content is MediaContent mediaContent)
            {
                FlushTextAndProfileContents();
#if ANDROID
                mediaContents.Add(mediaContent);
#else
                if (postType != PostType.Unwrapped) mediaContents.Add(mediaContent);
                else contentViewModels.Add(new MediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost));
#endif
            }
        }

        // Flush remaining contents
        FlushTextAndProfileContents();
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

        var textAndProfileContent = contents.Where(x => x is TextContent || x is ProfileContent);

        var firstContent = textAndProfileContent.FirstOrDefault();
        if (firstContent is TextContent firstTextContent) firstTextContent.Text = firstTextContent.Text.TrimStart();

        var lastContent = textAndProfileContent.LastOrDefault();
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
        else if (time.TotalHours < 24)
            result = $"{time.TotalHours:N0}시간 전";
        else if (createdAt.Year == DateTime.UtcNow.Year) result = $"{createdAt.ToLocalTime():MM월 dd일 HH:mm}";
        else result = $"{createdAt.ToLocalTime():yyyy년 MM월dd일 HH:mm:ss}";

        if (modifiedAt != null) result += $" (수정됨)";

        return result;
    }

    public static string GenerateTextPreviewFromContents(IEnumerable<BaseContent> contents)
    {
        var textAndProfileContents = contents.Where(x => x is TextContent || x is ProfileContent);

        var builder = new StringBuilder();
        foreach (var content in textAndProfileContents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
        }

        var result = builder.ToString();
        result = result.ReplaceLineEndings("\n");
        while (result.Contains("\n\n")) result = result.Replace("\n\n", "\n");
        return result;
    }

    public static string GenerateThumbnailUrlFromContents(IEnumerable<BaseContent> contents)
    {
        string imageUrl = null;

        var mediaId = contents.OfType<MediaContent>().Select(x => x.ThumbnailMediaId).FirstOrDefault();
        if (mediaId == null) mediaId = contents.OfType<ExternalUrlContent>().Select(x => x.ThumbnailImageUrl).FirstOrDefault();

        if (mediaId != null) imageUrl = GenerateMediaUri(mediaId);

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
        else if (string.IsNullOrWhiteSpace(preview) && post.Hashtags.Count > 0) preview = string.Join(" ", post.Hashtags.Select(x => $"#{x}"));
        return preview;
    }

    public static FormattedString GenerateSpanFromTextAndProfileContents(List<BaseContent> contents, PostType postType, bool hasMedias)
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
        }

        return formattedString;
    }

    public static AppTheme GetGlobalAppTheme()
    {
        var theme = Application.Current.UserAppTheme;
        if (theme == AppTheme.Unspecified) theme = Application.Current.PlatformAppTheme;
        else if (theme == AppTheme.Light) theme = AppTheme.Light;
        else if (theme == AppTheme.Dark) theme = AppTheme.Light;
        return theme;
    }

    private static void AddTapGestureRecognizerToLinkSpan(Span linkSpan, string url)
    {
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => Launcher.Default.OpenAsync(url);

        linkSpan.GestureRecognizers.Add(tapGesture);
    }

    private static void AddTapGestureRecognizerToProfileContentSnap(Span span, string userId)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushAsync(new UserPage(userId));
        span.GestureRecognizers.Add(tapGestureRecognizer);
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
            var shouldDownload = await App.Page.DisplayAlert("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})\n업데이트 하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!shouldDownload) return;
            var downloadUrl = "https://kagamine-rin.com/History/com.airtaxi.history-Signed.apk";
            var apkFilePath = Path.Combine(FileSystem.CacheDirectory, "History.apk");

            await Toast.Make("업데이트를 다운로드 중입니다. 잠시만 기다려 주세요.").Show();
            await Downloader.DownloadFileAsync(downloadUrl, apkFilePath);

            var context = Platform.CurrentActivity ?? Android.App.Application.Context;

            var file = new Java.IO.File(apkFilePath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);

#pragma warning disable CA1422 // Validate platform compatibility
            var intent = new Intent(Intent.ActionInstallPackage);
#pragma warning restore CA1422 // Validate platform compatibility
            intent.SetData(uri);
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            context.StartActivity(intent);
#else
            await App.Page.DisplayAlert("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})", Constants.PromptOk);
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"History Update Error: {ex.Message}");
            await App.Page.DisplayAlert("오류", $"업데이트 중 문제가 발생했습니다: {ex.Message}", "확인");
        }
    }
}
