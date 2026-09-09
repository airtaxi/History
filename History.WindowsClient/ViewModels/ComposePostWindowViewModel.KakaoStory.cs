using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;

namespace History.WindowsClient.ViewModels;

// Kakao Story cross-post surface of the composer: ordinary posts are written to
// Kakao Story first so a failure leaves nothing published, while fortune-only
// posts (#오늘의운세) are written to History first because the server generates
// their contents on write.
public sealed partial class ComposePostWindowViewModel : BaseViewModel
{
    // Kakao cross-post toggle; the saved session is validated before the mirror upload.
    [ObservableProperty]
    public partial bool IsKakaoPostEnabled { get; set; }

    // Blocks the submit when the Kakao Story text limit would be exceeded so the
    // mirror never fails after the History write.
    private async Task<bool> TryValidateKakaoStoryMirrorAsync(List<BaseContent> contents)
    {
        var textLength = GetKakaoStoryText(contents).Length;
        if (textLength <= 4000) return true;

        await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리의 글자 수 제한은 4,000자입니다. 현재 작성하신 게시글은 {textLength}자로 제한을 초과하여 카카오스토리에 게시할 수 없습니다. 게시글 내용을 수정하신 후 다시 시도해 주세요."));
        return false;
    }

    // Posts the given contents to Kakao Story with the composer's current media,
    // audience, and share setting. Images skipped by the mirror are reported after
    // the attempt so the user knows the Kakao Story post is missing some of them.
    private async Task<KakaoStoryMirrorResult> TryWriteKakaoStoryPostAsync(List<BaseContent> contents)
    {
        ShowLoading("카카오스토리에 게시하는 중...");
        try
        {
            var mirrorResult = await KakaoStoryUtils.WriteMirrorPostAsync(contents, MediaAttachments, ExternalUrlContent, SelectedDiscoveryOption, IsShareRepostDisallowed);
            if (mirrorResult.IsSuccess && mirrorResult.SkippedImageCount > 0) await ShowMessageDialogAsync(new MessageDialogParameters("카카오 게시", $"{mirrorResult.SkippedImageCount}개의 이미지가 카카오스토리 게시글에서 제외되었습니다. (형식 변환 실패 또는 이미지 수 제한 20개 초과)"));
            return mirrorResult;
        }
        finally { HideLoading(); }
    }

    // Detects a fortune-only post, i.e. a single #오늘의운세 hashtag optionally
    // accompanied by blank text. The server replaces its contents with the generated
    // fortune message, so the Kakao Story mirror must use the write response.
    private static bool IsFortuneOnlyPost(List<BaseContent> contents)
    {
        var fortuneHashtag = contents.OfType<HashtagContent>().FirstOrDefault(x => x.Tag == "오늘의운세");
        return fortuneHashtag != null && contents.All(x => x is HashtagContent || (x is TextContent blankText && string.IsNullOrWhiteSpace(blankText.Text)));
    }

    // Builds a plain-text representation from the write response contents, used for
    // the after-the-fact Kakao Story mirroring of fortune posts.
    private static string BuildKakaoStoryTextFromPostContents(List<BaseContent> contents)
    {
        var textBuilder = new StringBuilder();
        foreach (var content in contents)
        {
            if (content is TextContent text) textBuilder.Append(text.Text);
            else if (content is HashtagContent hashtag) textBuilder.Append('#').Append(hashtag.Tag).Append(' ');
        }
        return textBuilder.ToString().Trim();
    }

    // Builds a plain-text representation from the editor contents, used for the
    // Kakao Story 4,000-character limit check.
    private static string GetKakaoStoryText(List<BaseContent> contents)
    {
        var textBuilder = new StringBuilder();
        foreach (var content in contents)
        {
            if (content is TextContent text) textBuilder.Append(text.Text);
            else if (content is HashtagContent hashtag) textBuilder.Append('#').Append(hashtag.Tag).Append(' ');
            else if (content is ProfileContent profile) textBuilder.Append('@').Append(profile.Nickname).Append(' ');
            else if (content is HyperlinkContent hyperlink) textBuilder.Append(hyperlink.Url).Append(' ');
        }
        return textBuilder.ToString().Trim();
    }
}