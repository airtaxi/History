using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
        tapGestureRecognizer.Tapped += (s, e) => App.PushModalAsync(new UserPage(userId));
        span.GestureRecognizers.Add(tapGestureRecognizer);
    }
}
