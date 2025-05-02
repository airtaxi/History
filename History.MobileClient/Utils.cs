using AndroidX.Media3.Common;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient;

public static class Utils
{
    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }

    public static List<IContentViewModel> GenerateContentViewModels(IEnumerable<BaseContent> contents, bool wrapMedias)
    {
        var contentViewModels = new List<IContentViewModel>();

        var mediaContents = new List<MediaContent>();
        var allMediaContents = contents.OfType<MediaContent>();
        void FlushMediaContents()
        {
            if (mediaContents.Count > 0)
            {
                contentViewModels.Add(new MediaContentsViewModel(mediaContents, allMediaContents));
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
                if (wrapMedias) mediaContents.Add(mediaContent);
                else contentViewModels.Add(new MediaContentViewModel(mediaContent, allMediaContents));
            }
        }

        // Flush remaining contents
        FlushTextAndProfileContents();
        FlushMediaContents();

        return contentViewModels;
    }

    public static void TrimContents(List<BaseContent> contents)
    {
        var textContents = contents.OfType<TextContent>();
        var textOrProfileContents = contents.Where(x => x is TextContent || x is ProfileContent);
        do
        {
            if (textOrProfileContents.FirstOrDefault() is TextContent firstTextContent) firstTextContent?.Text = firstTextContent.Text.TrimStart();
            if (textOrProfileContents.LastOrDefault() is TextContent lastTextContent) lastTextContent?.Text = lastTextContent?.Text.TrimEnd();
            contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrEmpty(textContent.Text));
        }
        while (string.IsNullOrWhiteSpace(textContents.FirstOrDefault()?.Text) || string.IsNullOrWhiteSpace(textContents.LastOrDefault()?.Text));

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

    public static IMediaViewModel GenerateMediaViewModelFromMediaContent(MediaContent mediaContent, bool isInCarouselView) => mediaContent.IsVideo
    ? new VideoViewModel(CommonsConstants.MediaBaseUrl + mediaContent.MediaId)
    {
        VideoShouldShowPlaybackControls = false,
        Aspect = Aspect.AspectFill,
        ShouldMute = true,
		ResizeParentCarouselViewWhenSizeChanged = isInCarouselView && false,
		HorizontalContentOptions = LayoutOptions.Fill,
        VideoShouldAutoPlay = true,
        VideoShouldLoopPlayback = true,
        VerticalContentOptions = LayoutOptions.Fill
    }
    : new ImageViewModel(CommonsConstants.MediaBaseUrl + mediaContent.MediaId)
    {
        Aspect = Aspect.AspectFill,
		ResizeParentCarouselViewWhenSizeChanged = isInCarouselView && false,
		HorizontalContentOptions = LayoutOptions.Fill,
        VerticalContentOptions = LayoutOptions.Fill
    };

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

    private static void AddTapGestureRecognizerToProfileContentSnap(Span span, string userId)
    {
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += async (s, e) => await App.PushModalAsync(new UserPage(userId));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }
}
