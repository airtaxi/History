using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;

namespace History.MobileClient;

public static class Utils
{
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
                contentViewModels.Add(new TextAndProfileContentsViewModel(textAndProfileContents));
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
        if (firstTextContent != null) firstTextContent.Text = textContents.FirstOrDefault().Text.TrimStart();

        var lastTextContent = textContents.LastOrDefault();
        if (lastTextContent != null) lastTextContent.Text = textContents.LastOrDefault().Text.TrimEnd();

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

    public static FormattedString GenerateSpanFromTextAndProfileContents(List<BaseContent> contents)
    {
        var formattedString = new FormattedString();
        foreach (var content in contents)
        {
            if (content is TextContent textContent)
            {
                formattedString.Spans.Add(new Span
                {
                    Text = textContent.Text,
                });
            }
            else if (content is ProfileContent profileContent)
            {
                var span = new Span
                {
                    Text = profileContent.Nickname,
                    TextColor = Application.Current.Resources["Primary"] as Color,
                    FontAttributes = FontAttributes.Bold,
                };

                if (profileContent.UserId != null) 
                    AddTapGestureRecognizerToProfileContentSnap(span, profileContent.UserId);

                formattedString.Spans.Add(span);
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

    private static void AddTapGestureRecognizerToProfileContentSnap(Span span, string userId)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushModalAsync(new UserPage(userId));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }
}
