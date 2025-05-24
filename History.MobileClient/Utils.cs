using History.Commons;
using History.Commons.Api.PushNotification;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Plugin.Firebase.CloudMessaging;
using System.Text.RegularExpressions;

namespace History.MobileClient;

public static class Utils
{
    private const int TimelineMaxTextLengthWithoutMedias = 600;
    private const int TimelineMaxTextLengthWithMedias = 100;

    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }

    public static List<IContentViewModel> GenerateContentViewModels(IEnumerable<BaseContent> contents, bool isTimeline)
    {
        var contentViewModels = new List<IContentViewModel>();

        var mediaContents = new List<MediaContent>();
        var allMediaContents = contents.OfType<MediaContent>();
        void FlushMediaContents()
        {
            if (mediaContents.Count > 0)
            {
                contentViewModels.Add(new WrappedMediaContentsViewModel(mediaContents, allMediaContents));
                mediaContents = [];
            }
        }

        var textAndProfileContents = new List<BaseContent>();
        void FlushTextAndProfileContents()
        {
            if (textAndProfileContents.Count > 0)
            {
                contentViewModels.Add(new TextAndProfileContentsViewModel(textAndProfileContents, isTimeline, contents.OfType<MediaContent>().Any() || contents.OfType<ExternalUrlContent>().Any()));
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
                if (isTimeline) mediaContents.Add(mediaContent);
                else contentViewModels.Add(new MediaContentViewModel(mediaContent, allMediaContents, false));
            }
        }

        // Flush remaining contents
        FlushTextAndProfileContents();
        FlushMediaContents();

        return contentViewModels;
    }

    public static void TrimContents(List<BaseContent> contents)
    {
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

        var textContents = contents.OfType<TextContent>();

        var firstTextContent = textContents.FirstOrDefault();
        if (firstTextContent != null) firstTextContent.Text = firstTextContent.Text.TrimStart();

        var lastTextContent = textContents.LastOrDefault();
        if (lastTextContent != null) lastTextContent.Text = lastTextContent.Text.TrimEnd();

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
        else if (createdAt.Year == DateTime.UtcNow.Year) result = $"{createdAt:MM월 dd일 HH:mm}";
        else result = $"{createdAt.ToLocalTime():yyyy년 MM월dd일 HH:mm:ss}";

        if (modifiedAt != null) result += $" (수정됨)";

        return result;
    }

    public static FormattedString GenerateSpanFromTextAndProfileContents(List<BaseContent> contents, bool isTimeline, bool hasMedias)
    {
        var formattedString = new FormattedString();
        var maxLength = hasMedias ? TimelineMaxTextLengthWithMedias : TimelineMaxTextLengthWithoutMedias;
        var currentLength = 0;

        var urlRegex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled);

        foreach (var content in contents)
        {
            if (isTimeline && currentLength > maxLength)
            {
                formattedString.Spans.Add(new Span
                {
                    Text = " ... 더보기",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromRgb(0x99, 0x99, 0x99)
                });
                break;
            }

            if (content is TextContent textContent)
            {
                var matches = urlRegex.Matches(textContent.Text);
                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        string plainText = textContent.Text[lastIndex..match.Index];

                        var span = new Span { Text = plainText };
                        formattedString.Spans.Add(span);

                        currentLength += plainText.Length;
                        if (isTimeline && currentLength > maxLength)
                        {
                            span.Text = span.Text[..maxLength];
                            break;
                        }
                    }

                    string url = match.Value;

                    var linkSpan = new Span
                    {
                        Text = url,
                        TextColor = Application.Current.Resources["Primary"] as Color
                    };
                    AddTapGestureRecognizerToLinkSpan(linkSpan, url);
                    formattedString.Spans.Add(linkSpan);

                    lastIndex = match.Index + match.Length;

                    currentLength += url.Length;
                    if (isTimeline && currentLength > maxLength)
                    {
                        linkSpan.Text = linkSpan.Text[..maxLength];
                        break;
                    }
                }

                if (lastIndex < textContent.Text.Length)
                {
                    string remaining = textContent.Text[lastIndex..];

                    var span = new Span { Text = remaining };
                    formattedString.Spans.Add(span);

                    currentLength += remaining.Length;
                    if (isTimeline && currentLength > maxLength)
                    {
                        span.Text = span.Text[..maxLength];
                        break;
                    }
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
                formattedString.Spans.Add(span);

                if (profileContent.UserId != null) AddTapGestureRecognizerToProfileContentSnap(span, profileContent.UserId);

                currentLength += profileContent.Nickname.Length;
                if (isTimeline && currentLength > maxLength)
                {
                    span.Text = span.Text[..maxLength];
                    break;
                }
            }
        }

        return formattedString;
    }

    public static AppTheme GetGlobalAppTheme()
    {
        var theme = Application.Current.UserAppTheme;
        if (theme == AppTheme.Unspecified) theme = Application.Current.PlatformAppTheme;
        if (theme == AppTheme.Unspecified) theme = AppTheme.Light;
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
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushModalAsync(new UserPage(userId));
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
}
