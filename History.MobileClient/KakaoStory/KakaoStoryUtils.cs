using History.Commons.Api.KakaoStory;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.KakaoStory;
using History.Commons.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Graphics.Platform;
using UraniumUI.Icons.FontAwesome;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
using History.Commons;

namespace History.MobileClient.KakaoStory;

public partial class KakaoStoryUtils : CommonKakaoStoryUtils
{
    private static bool s_isRelogging;

    // Kakao Story emotions map to the History reaction visuals (glyph + color).
    // Emotion strings: like(좋아요), good(멋져요), pleasure(기뻐요), sad(슬퍼요), cheerup(힘내요).
    public static (string Glyph, Color Color) GetEmotionVisual(string emotion)
    {
        return emotion switch
        {
            "like" => (Solid.Heart, Color.FromRgb(0xEB, 0x55, 0x27)),
            "good" => (Solid.Star, Color.FromRgb(0xBB, 0xCC, 0x29)),
            "pleasure" => (Solid.FaceSmile, Color.FromRgb(0xFF, 0xC1, 0x00)),
            "sad" => (Solid.Droplet, Color.FromRgb(0x00, 0x9F, 0xB2)),
            "cheerup" => (Solid.Bolt, Color.FromRgb(0xA0, 0x61, 0xB1)),
            _ => (Solid.Heart, Color.FromRgb(0xEB, 0x55, 0x27))
        };
    }

    /// <summary>
    /// Relogin entry point used by KakaoStoryApiHandler when a request returns 401.
    /// Presents the login modal (or auto-fill) and updates the stored cookies.
    /// Returns true when a valid session is available afterwards.
    /// Re-entrancy guard: the cookie validation inside EnsureLoggedInAsync also performs
    /// API requests, which 401 again and would otherwise recurse into this method forever.
    /// Nested invocations return false and let the outer relogin flow show the login page.
    /// </summary>
    public static async Task<bool> ReLoginAsync()
    {
        if (s_isRelogging) return false;
        s_isRelogging = true;
        try
        {
            var success = await EnsureLoggedInAsync(App.TopPage);
            if (success)
            {
                // Refresh the cached user id after relogin so post action sheets stay accurate.
                await SaveCurrentUserAsync();
                // Re-register the server session with the fresh token.
                await UploadTokenToServerAsync();
            }
            return success;
        }
        finally { s_isRelogging = false; }
    }

    /// <summary>
    /// Validates the saved SDK tokens (KAuth) and, when they are missing/expired,
    /// presents the KakaoStoryLoginPage modal. Returns true when a valid session
    /// is available afterwards. When the session is already valid, the friends
    /// and user-id caches are refreshed only when they are empty (cold start or
    /// an earlier cache wipe); otherwise routine navigation costs no extra requests.
    /// The login page itself refreshes friends/token on success, so the modal
    /// path needs no additional reload here.
    /// </summary>
    public static async Task<bool> EnsureLoggedInAsync(Page hostPage)
    {
        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() != null)
        {
            if (CommonShared.KakaoFriends == null || CommonShared.KakaoUserId == null)
            {
                await RefreshSessionCachesAsync();
                return true;
            }

            _ = KakaoStoryApiHandler.EnsureEmoticonCredentialAsync(); // Warm up so first emoticons render immediately.
            return true;
        }

        return await ShowLoginModalAsync(hostPage);
    }

    /// <summary>
    /// Presents the auto-fill prompt (when no credential is saved) and the login modal.
    /// The login page refreshes friends and uploads the poll token on success.
    /// </summary>
    private static async Task<bool> ShowLoginModalAsync(Page hostPage)
    {
        var savedEmail = await KakaoStoryCredentialStore.GetEmailAsync();
        var savedPassword = await KakaoStoryCredentialStore.GetPasswordAsync();
        if (savedEmail == null || savedPassword == null)
        {
            var useAutoFill = await hostPage.DisplayAlertAsync("자동 입력", "세션이 만료되어 로그인이 필요합니다. 카카오스토리 로그인 정보를 저장하여 자동 입력하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (useAutoFill)
            {
                var email = await hostPage.DisplayPromptAsync("이메일 입력", "카카오 계정 이메일을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "이메일", -1, Keyboard.Email);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var password = await hostPage.DisplayPromptAsync("비밀번호 입력", "카카오 계정 비밀번호를 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "비밀번호", -1, Keyboard.Password);
                    if (!string.IsNullOrWhiteSpace(password)) await KakaoStoryCredentialStore.SaveAsync(email, password);
                }
            }
        }

        var kakaoStoryLoginPage = new KakaoStoryLoginPage();
        await App.PushModalAsync(kakaoStoryLoginPage);

        return await kakaoStoryLoginPage.GetResultAsync();
    }

    /// <summary>
    /// Shows the Kakao Story-only write mirroring guide once per install. The guide
    /// explains that the post is mirrored to History after the Kakao Story upload,
    /// and how Kakao Story mentions are resolved on the History side.
    /// </summary>
    public static async Task ShowKakaoOnlyWriteGuideOnceAsync(Page hostPage)
    {
        if (Configuration.GetValue<bool?>("KakaoOnlyWriteGuideDismissed") ?? false) return;
        var showAgain = await hostPage.DisplayAlertAsync("안내", KakaoOnlyWriteGuideMessage, Constants.PromptOk, "다시 보지 않기");
        if (!showAgain) Configuration.SetValue("KakaoOnlyWriteGuideDismissed", true);
    }

    /// <summary>
    /// Builds contents with Kakao Story emoticons rendered as sticker view models
    /// (Referer-signed images). Text decorators are batched into
    /// TextTypeContentsViewModel; emoticons whose signed URL cannot be resolved
    /// keep the "(이모티콘)" text placeholder. Returns null when the input has no
    /// emoticon decorators — the caller then keeps its existing contents.
    /// </summary>
    public static async Task<List<IContentViewModel>> BuildEmoticonContentsAsync(List<QuoteData> quoteDatas, PostType postType)
    {
        if (quoteDatas == null || !quoteDatas.Any(x => x.type == "emoticon")) return null;

        await KakaoStoryApiHandler.EnsureEmoticonCredentialAsync();

        var contents = new List<IContentViewModel>();
        var textBatch = new List<QuoteData>();
        foreach (var data in quoteDatas)
        {
            if (data.type == "emoticon")
            {
                if (textBatch.Count > 0)
                {
                    contents.Add(new TextTypeContentsViewModel(textBatch, postType));
                    textBatch = [];
                }

                var emoticonUrl = KakaoStoryApiHandler.GetEmoticonUrlSync(data.item_id, data.resource_id.ToString());
                if (emoticonUrl == null) contents.Add(new TextTypeContentsViewModel([new QuoteData { type = "text", text = "(이모티콘) " }], postType));
                else contents.Add(new StickerContentViewModel(emoticonUrl, postType));
            }
            else textBatch.Add(data);
        }
        if (textBatch.Count > 0) contents.Add(new TextTypeContentsViewModel(textBatch, postType));

        return contents;
    }
    /// <summary>
    /// Creates the post view model for a Kakao Story feed item, unwrapping bundled feeds
    /// (share/UP activities) into the shared post/repost surfaces (WPF pattern):
    /// - bundled_feed.type == "up"    -> render the original activity as a repost card.
    /// - bundled_feed.type == "share" -> inject the original activity into activities[0].@object
    ///                                    so the shared card renders the original post.
    /// - bundled_feed.type == "scrap" -> render only the most recent activity
    ///                                    (bundled_feed.activity) as a normal link-embedded post.
    /// Returns null when the post author is banned (relation.ban == "A") so callers skip it.
    /// Also returns null for the verbs in <see cref="CommonKakaoStoryUtils.NonPostVerbs"/>, which are not a
    /// user's post at all, and for a bundled_feed whose type has no rule here.
    /// </summary>
    public static BasePostViewModel CreatePostViewModel(PostData postData)
    {
        if (postData.actor?.relation?.ban == "A") return null;

        if (postData.verb != null && NonPostVerbs.Contains(postData.verb)) return null;

        var bundledFeed = postData.bundled_feed;
        if (postData.verb == "bundled_feed")
        {
            // The wrapper carries no content of its own, so a bundle this method has
            // no rule for would render as an empty card.
            if (bundledFeed == null) return null;

            // The repost card renders the original activity's content, so the original
            // author is also checked for a ban (relation.ban == "A").
            if (bundledFeed.type == "up" && bundledFeed.original_activity != null)
            {
                if (bundledFeed.original_activity.actor?.relation?.ban == "A") return null;
                return new KakaoRepostViewModel(postData);
            }

            if (bundledFeed.type == "share" && bundledFeed.activities is { Count: > 0 })
            {
                var activity = bundledFeed.activities[0];
                activity.@object = bundledFeed.original_activity;
                return new KakaoPostViewModel(activity);
            }

            // bundled_feed.type == "scrap" -> N people shared the same link; render only the
            // most recent activity (bundled_feed.activity) as a normal link-embedded post.
            if (bundledFeed.type == "scrap")
            {
                var activity = bundledFeed.activity ?? bundledFeed.activities?.FirstOrDefault();
                if (activity == null) return null;
                if (activity.actor?.relation?.ban == "A") return null;
                return new KakaoPostViewModel(activity);
            }

            return null;
        }

        return new KakaoPostViewModel(postData);
    }

    /// <summary>
    /// Converts the picked image to PNG when Kakao Story does not accept the format.
    /// WebP is converted (Kakao Story does not support it); GIF is rejected because
    /// only static images are allowed. Returns null when the image cannot be used.
    /// </summary>
    public static async Task<byte[]> TryConvertToKakaoSupportedImageAsync(string fileName, byte[] bytes)
    {
        if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            await App.Page.DisplayAlertAsync("안내", "움직이는 이미지(gif)는 프로필 이미지로 설정할 수 없습니다.", Constants.PromptOk);
            return null;
        }

        if (!fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return bytes;

        try
        {
            using var stream = new MemoryStream(bytes);
            using var image = PlatformImage.FromStream(stream);
            if (image == null)
            {
                await App.Page.DisplayAlertAsync("오류", "이미지를 변환할 수 없습니다. 애니메이션이 포함된 webp 이미지일 수 있습니다.", Constants.PromptOk);
                return null;
            }

            using var saveStream = new MemoryStream();
            await image.SaveAsync(saveStream, ImageFormat.Png);
            return saveStream.ToArray();
        }
        catch
        {
            await App.Page.DisplayAlertAsync("오류", "이미지를 변환할 수 없습니다. 애니메이션이 포함된 webp 이미지일 수 있습니다.", Constants.PromptOk);
            return null;
        }
    }

    /// <summary>
    /// Returns whether the image file must be converted to PNG before a KakaoStory upload
    /// because KakaoStory does not accept the format (webp, heic, heif, avif).
    /// </summary>
    public static bool IsKakaoStoryUnsupportedImageFormat(string fileName) =>
        fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".heif", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase);
}
