using System.Net;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Models;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.WindowsClient.ViewModels;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.Helpers;

public partial class KakaoStoryUtils : CommonKakaoStoryUtils
{
    private static bool s_isRelogging;

    // Relogin entry point used by KakaoStoryApiHandler when a request returns 401.
    // Presents the login window (or auto-fill) and updates the saved session state.
    // Returns true when a valid session is available afterwards.
    // Re-entrancy guard: the cookie validation inside EnsureLoggedInAsync also performs
    // API requests, which 401 again and would otherwise recurse into this method forever.
    // Nested invocations return false and let the outer relogin flow show the login window.
    public static async Task<bool> ReLoginAsync()
    {
        if (s_isRelogging) return false;
        s_isRelogging = true;
        try
        {
            var success = await EnsureLoggedInAsync();
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

    private static readonly object s_loginLock = new();
    private static Task<bool> s_pendingLoginTask;

    // Validates the saved SDK tokens (KAuth) and, when they are missing/expired,
    // presents the KakaoStoryLoginWindow. Returns true when a valid session is
    // available afterwards. When the session is already valid, the friends and
    // user-id caches are refreshed only when they are empty (cold start or an
    // earlier cache wipe); otherwise routine navigation costs no extra requests.
    // Concurrent callers (for example the main page and the timeline during startup)
    // share one login window instead of opening a modal per caller.
    public static async Task<bool> EnsureLoggedInAsync(BaseViewModel baseViewModel = null)
    {
        if (await ValidateSessionCachesAsync()) return true;

        Task<bool> loginTask;
        lock (s_loginLock)
        {
            if (s_pendingLoginTask is null || s_pendingLoginTask.IsCompleted) s_pendingLoginTask = ShowLoginModalAsync(baseViewModel);
            loginTask = s_pendingLoginTask;
        }

        return await loginTask;
    }

    // Checks the saved session and refreshes the friends/user-id caches only when they
    // are empty. Returns false when no valid session is available.
    private static async Task<bool> ValidateSessionCachesAsync()
    {
        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() == null) return false;

        if (CommonShared.KakaoFriends == null || CommonShared.KakaoUserId == null)
        {
            await RefreshSessionCachesAsync();
            return true;
        }

        _ = KakaoStoryApiHandler.EnsureEmoticonCredentialAsync(); // Warm up so first emoticons render immediately.
        return true;
    }

    // Presents the auto-fill prompt (when no credential is saved) and the login window.
    // The login window refreshes friends and uploads the poll token on success.
    private static async Task<bool> ShowLoginModalAsync(BaseViewModel baseViewModel = null)
    {
        // 401 responses can arrive from background threads; the dialogs and the modal
        // window must run on the UI thread.

        if (baseViewModel == null)
        {

            var frame = MainWindow.Frame;
            if (!frame.DispatcherQueue.HasThreadAccess)
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                frame.DispatcherQueue.TryEnqueue(async () => taskCompletionSource.TrySetResult(await ShowLoginModalOnUiThreadAsync(frame)));
                return await taskCompletionSource.Task;
            }

            return await ShowLoginModalOnUiThreadAsync(frame);
        }
        else return await ShowLoginModalOnUiThreadAsync(baseViewModel);
    }

    private static async Task<bool> ShowLoginModalOnUiThreadAsync(Frame frame)
    {
        var savedEmail = await KakaoStoryCredentialStore.GetEmailAsync();
        var savedPassword = await KakaoStoryCredentialStore.GetPasswordAsync();
        if (savedEmail == null || savedPassword == null)
        {
            var useAutoFill = await frame.ShowMessageDialogAsync(new MessageDialogParameters("자동 입력", "세션이 만료되어 로그인이 필요합니다. 카카오스토리 로그인 정보를 저장하여 자동 입력하시겠습니까?", "확인", "취소"));
            if (useAutoFill == ContentDialogResult.Primary)
            {
                var email = await frame.ShowInputDialogAsync(new InputDialogParameters("이메일 입력", "카카오 계정 이메일을 입력해주세요.", "이메일", showCancel: true));
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var password = await frame.ShowInputDialogAsync(new InputDialogParameters("비밀번호 입력", "카카오 계정 비밀번호를 입력해주세요.", "비밀번호", showCancel: true));
                    if (!string.IsNullOrWhiteSpace(password)) await KakaoStoryCredentialStore.SaveAsync(email, password);
                }
            }
        }

        var loginWindow = new KakaoStoryLoginWindow(new KakaoStoryLoginWindowViewModel());
        loginWindow.MakeModal(MainWindow.Instance);

        return await loginWindow.GetResultAsync();
    }

    private static async Task<bool> ShowLoginModalOnUiThreadAsync(BaseViewModel baseViewModel)
    {
        var savedEmail = await KakaoStoryCredentialStore.GetEmailAsync();
        var savedPassword = await KakaoStoryCredentialStore.GetPasswordAsync();
        if (savedEmail == null || savedPassword == null)
        {
            var useAutoFill = await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("자동 입력", "세션이 만료되어 로그인이 필요합니다. 카카오스토리 로그인 정보를 저장하여 자동 입력하시겠습니까?", "확인", "취소"));
            if (useAutoFill == ContentDialogResult.Primary)
            {
                var email = await baseViewModel.ShowInputDialogAsync(new InputDialogParameters("이메일 입력", "카카오 계정 이메일을 입력해주세요.", "이메일", showCancel: true));
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var password = await baseViewModel.ShowInputDialogAsync(new InputDialogParameters("비밀번호 입력", "카카오 계정 비밀번호를 입력해주세요.", "비밀번호", showCancel: true));
                    if (!string.IsNullOrWhiteSpace(password)) await KakaoStoryCredentialStore.SaveAsync(email, password);
                }
            }
        }

        var loginWindow = new KakaoStoryLoginWindow(new KakaoStoryLoginWindowViewModel());
        loginWindow.MakeModal(MainWindow.Instance);

        return await loginWindow.GetResultAsync();
    }

    // Posts the given contents to Kakao Story on behalf of a History post: converts
    // the editor contents to QuoteData, uploads stickers/photos/videos (converting
    // unsupported image formats to PNG), attaches the external URL as a scrap card
    // when no media is present (otherwise as trailing text), and maps the discovery
    // option to the Kakao Story permission. Images that exceed the Kakao Story limit
    // or fail to convert are skipped and counted. Returns the upload outcome.
    public static async Task<KakaoStoryMirrorResult> WriteMirrorPostAsync(List<BaseContent> contents, IEnumerable<MediaAttachmentViewModel> attachments, ExternalUrlContent externalUrlContent, DiscoveryOption discoveryOption, bool disallowShare)
    {
        var skippedImageCount = 0;
        try
        {
            var quoteDatas = GetQuoteDataFromContents(contents);
            var medias = new List<MediaData.MediaObject>();
            var uploadedImageCount = 0;

            // Stickers are uploaded as images ahead of the photos.
            foreach (var stickerContent in contents.OfType<StickerContent>())
            {
                if (uploadedImageCount >= CommonConstants.KakaoStoryMaxImageCount) break;
                if (stickerContent.StickerMediaId == null) continue;

                var stickerData = await CommonUtils.GetStickerImageDataAsync(stickerContent.StickerMediaId);
                if (stickerData is not { Length: > 0 }) continue;

                var convertedData = ImageConversionHelper.ConvertToPng(stickerData);
                if (convertedData == null) continue;

                var stickerFilePath = Path.Combine(Path.GetTempPath(), $"kakaostory_sticker_{Guid.NewGuid():N}.png");
                try
                {
                    await File.WriteAllBytesAsync(stickerFilePath, convertedData);
                    var stickerMediaPath = await KakaoStoryApiHandler.UploadImage(stickerFilePath);
                    medias.Add(new MediaData.MediaObject { media_path = stickerMediaPath, media_type = "image" });
                    uploadedImageCount++;
                }
                finally { TryDeleteTempFile(stickerFilePath); }
            }

            foreach (var attachment in attachments)
            {
                if (!attachment.IsVideo)
                {
                    if (uploadedImageCount >= CommonConstants.KakaoStoryMaxImageCount) { skippedImageCount++; continue; }

                    var uploadPath = attachment.FilePath;
                    var convertedFilePath = (string)null;
                    if (IsKakaoStoryUnsupportedImageFormat(attachment.FileName))
                    {
                        var convertedData = ImageConversionHelper.ConvertToPng(attachment.FilePath);
                        if (convertedData == null) { skippedImageCount++; continue; }

                        convertedFilePath = Path.Combine(Path.GetTempPath(), $"kakaostory_photo_{Guid.NewGuid():N}.png");
                        await File.WriteAllBytesAsync(convertedFilePath, convertedData);
                        uploadPath = convertedFilePath;
                    }

                    try
                    {
                        var mediaPath = await KakaoStoryApiHandler.UploadImage(uploadPath);
                        medias.Add(new MediaData.MediaObject { media_path = mediaPath, media_type = "image", caption = BuildKakaoStoryMediaCaption(attachment.Description) });
                        uploadedImageCount++;
                    }
                    finally
                    {
                        if (convertedFilePath != null)
                        {
                            TryDeleteTempFile(convertedFilePath);
                        }
                    }
                }
                else
                {
                    if (!File.Exists(attachment.FilePath)) await File.WriteAllBytesAsync(attachment.FilePath, attachment.Data);
                    var videoAccessKey = await KakaoStoryApiHandler.UploadVideo(attachment.FilePath);
                    await KakaoStoryApiHandler.WaitForVideoUploadFinish(videoAccessKey);
                    medias.Add(new MediaData.MediaObject { media_path = videoAccessKey, media_type = "video", caption = BuildKakaoStoryMediaCaption(attachment.Description) });
                }
            }

            MediaData mediaData = null;
            if (medias.Count > 0)
            {
                mediaData = new MediaData { media = medias };
                var hasImage = medias.Any(x => x.media_type == "image");
                var hasVideo = medias.Any(x => x.media_type == "video");
                mediaData.media_type = hasImage && hasVideo ? "mixed" : hasImage ? "image" : "video";
            }

            // Kakao Story scrapes a URL only when no media is attached; otherwise the
            // URL is appended as plain text at the end of the body.
            var externalUrl = externalUrlContent?.SourceUrl;
            string scrap = null;
            if (mediaData == null && !string.IsNullOrWhiteSpace(externalUrl)) scrap = await GetScrapDataWithRetryAsync(externalUrl);
            else if (mediaData != null && !string.IsNullOrWhiteSpace(externalUrl)) quoteDatas.Add(new QuoteData { type = "text", text = $"\n\n{externalUrl}" });

            await KakaoStoryApiHandler.WritePost(quoteDatas, mediaData, MapDiscoveryOptionToKakaoPermission(discoveryOption), true, !disallowShare, null, null, scrap);
            return new KakaoStoryMirrorResult(true, null, skippedImageCount);
        }
        catch (Exception exception) { return new KakaoStoryMirrorResult(false, GetApiErrorMessage(exception), skippedImageCount); }
    }

    // Uploads a History sticker as a Kakao Story photo. History stickers are webp, which
    // Kakao Story rejects, so the image is converted to PNG first. Returns null when the
    // download, conversion, or upload fails so the caller can skip the sticker.
    public static async Task<MediaData.MediaObject> TryUploadStickerMediaAsync(string stickerMediaId)
    {
        var stickerData = await CommonUtils.GetStickerImageDataAsync(stickerMediaId);
        if (stickerData is not { Length: > 0 }) return null;

        var convertedData = ImageConversionHelper.ConvertToPng(stickerData);
        if (convertedData == null) return null;

        var stickerFilePath = Path.Combine(Path.GetTempPath(), $"kakaostory_sticker_{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(stickerFilePath, convertedData);
            var mediaPath = await KakaoStoryApiHandler.UploadImage(stickerFilePath);
            return new MediaData.MediaObject { media_path = mediaPath, media_type = "image" };
        }
        finally { TryDeleteTempFile(stickerFilePath); }
    }

    // Uploads a local photo attachment, converting formats Kakao Story rejects
    // (webp/heic/heif/avif) to PNG first. Returns null when the conversion fails.
    public static async Task<MediaData.MediaObject> TryUploadImageAttachmentAsync(MediaAttachmentViewModel attachment)
    {
        var uploadPath = attachment.FilePath;
        var convertedFilePath = (string)null;
        if (IsKakaoStoryUnsupportedImageFormat(attachment.FileName))
        {
            var convertedData = ImageConversionHelper.ConvertToPng(attachment.FilePath);
            if (convertedData == null) return null;

            convertedFilePath = Path.Combine(Path.GetTempPath(), $"kakaostory_photo_{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(convertedFilePath, convertedData);
            uploadPath = convertedFilePath;
        }

        try
        {
            var mediaPath = await KakaoStoryApiHandler.UploadImage(uploadPath);
            return new MediaData.MediaObject { media_path = mediaPath, media_type = "image", caption = BuildKakaoStoryMediaCaption(attachment.Description) };
        }
        finally
        {
            if (convertedFilePath != null)
            {
                TryDeleteTempFile(convertedFilePath);
            }
        }
    }

    // Uploads a local video attachment and waits for the server-side transcode to finish.
    public static async Task<MediaData.MediaObject> UploadVideoAttachmentAsync(MediaAttachmentViewModel attachment)
    {
        if (!File.Exists(attachment.FilePath)) await File.WriteAllBytesAsync(attachment.FilePath, attachment.Data);
        var videoAccessKey = await KakaoStoryApiHandler.UploadVideo(attachment.FilePath);
        await KakaoStoryApiHandler.WaitForVideoUploadFinish(videoAccessKey);
        return new MediaData.MediaObject { media_path = videoAccessKey, media_type = "video", caption = BuildKakaoStoryMediaCaption(attachment.Description) };
    }

    // Fetches the scrap payload for a URL, retrying transient scrape failures. Returns the
    // scrap JSON, or null when the scraper keeps returning an unusable response.
    public static async Task<string> GetScrapDataWithRetryAsync(string url)
    {
        var scrapTryCount = 0;
        const int scrapMaxRetryCount = 5;
        async Task<string> FetchScrapDataAsync()
        {
            string scrapData;
            try { scrapData = await KakaoStoryApiHandler.GetScrapData(url); }
            catch (WebException)
            {
                scrapTryCount++;
                if (scrapTryCount >= scrapMaxRetryCount) throw;
                else return await FetchScrapDataAsync();
            }

            if (!KakaoStoryApiHandler.IsScrapDataUsable(scrapData))
            {
                scrapTryCount++;
                if (scrapTryCount >= scrapMaxRetryCount) return null;
                else return await FetchScrapDataAsync();
            }
            return scrapData;
        }
        return await FetchScrapDataAsync();
    }

    // Maps a Kakao Story scrap card onto the shared external URL surface.
    public static ExternalUrlContent CreateExternalUrlContent(TimeLineData.Scrap scrap) => new()
    {
        Title = scrap.title,
        Description = scrap.description,
        SourceUrl = scrap.dest_url ?? scrap.url,
        ThumbnailImageUrl = scrap.image?.FirstOrDefault()
    };

    public static bool IsKakaoStoryUnsupportedImageFormat(string fileName) =>
        fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".heif", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase);

    // Maps a History discovery option to the Kakao Story permission value. Only
    // Everyone/OnlyMe have exact Kakao Story equivalents; everything else is friends.
    public static string MapDiscoveryOptionToKakaoPermission(DiscoveryOption discoveryOption)
    {
        if (discoveryOption == DiscoveryOption.Everyone) return "A";
        else if (discoveryOption == DiscoveryOption.OnlyMe) return "M";
        else return "F";
    }

    // Builds the Kakao Story media caption payload from a media description. An empty
    // description maps to an empty caption array (verified web request format).
    public static List<MediaData.CaptionData> BuildKakaoStoryMediaCaption(string description) => string.IsNullOrEmpty(description) ? [] : [new MediaData.CaptionData { text = description }];

    private static void TryDeleteTempFile(string filePath)
    {
        try { File.Delete(filePath); }
        catch { }
    }

    // Reads the Kakao Story API error body (when the failure is an HTTP error) so the
    // user sees the server's reason instead of a generic message.
    public static string GetApiErrorMessage(Exception exception)
    {
        if (exception is not WebException { Response: HttpWebResponse response }) return exception.Message;

        try
        {
            using var responseReader = new StreamReader(response.GetResponseStream());
            return $"[{(int)response.StatusCode}] {responseReader.ReadToEnd()}";
        }
        catch { return exception.Message; }
    }

    // Kakao Story emotions reuse the History reaction visuals (Segoe Fluent glyph and the
    // fixed History reaction palette). A null/unknown emotion returns the idle visual used
    // when the current user has not reacted to the post.
    public static (string Glyph, Color Color) GetEmotionVisual(string emotion) => emotion switch
    {
        "like" => ("\uEB52", Color.FromArgb(0xFF, 0xEB, 0x55, 0x27)),
        "good" => ("\uE735", Color.FromArgb(0xFF, 0xBB, 0xCC, 0x29)),
        "pleasure" => ("\uED54", Color.FromArgb(0xFF, 0xFF, 0xC1, 0x00)),
        "sad" => ("\uEB42", Color.FromArgb(0xFF, 0x00, 0x9F, 0xB2)),
        "cheerup" => ("\uE945", Color.FromArgb(0xFF, 0xA0, 0x61, 0xB1)),
        _ => ("\uEB51", (Color)Application.Current.Resources["ReverseThemeColor"]),
    };

    // Creates the post view model for a Kakao Story feed item, unwrapping bundled feeds
    // (share/UP activities) into the shared post/repost surfaces:
    // - bundled_feed.type == "up"    -> render the original activity as a repost card.
    // - bundled_feed.type == "share" -> inject the original activity into activities[0].@object
    //                                    so the shared card renders the original post.
    // - bundled_feed.type == "scrap" -> render only the most recent activity
    //                                    as a normal link-embedded post.
    // Returns null when the post author is banned (relation.ban == "A") so callers skip it,
    // and for verbs that are not a user's post (see CommonKakaoStoryUtils.NonPostVerbs).
    public static BasePostViewModel CreatePostViewModel(PostData postData, BaseViewModel baseViewModel)
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
                return new KakaoRepostViewModel(postData, PostType.Timeline, baseViewModel);
            }

            if (bundledFeed.type == "share" && bundledFeed.activities is { Count: > 0 })
            {
                var activity = bundledFeed.activities[0];
                activity.@object = bundledFeed.original_activity;
                return new KakaoPostViewModel(activity, PostType.Timeline, baseViewModel);
            }

            // bundled_feed.type == "scrap" -> N people shared the same link; render only the
            // most recent activity (bundled_feed.activity) as a normal link-embedded post.
            if (bundledFeed.type == "scrap")
            {
                var activity = bundledFeed.activity ?? bundledFeed.activities?.FirstOrDefault();
                if (activity == null) return null;
                if (activity.actor?.relation?.ban == "A") return null;
                return new KakaoPostViewModel(activity, PostType.Timeline, baseViewModel);
            }

            return null;
        }

        return new KakaoPostViewModel(postData, PostType.Timeline, baseViewModel);
    }

    // Sends a Kakao Story memo through the message endpoint, reporting the reason when the
    // API rejects the mail so the editor can keep the dialog open.
    public static async Task<Result> SendMailAsync(BaseViewModel baseViewModel, string receiverId, string text)
    {
        try
        {
            var success = await baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SendMail(text, receiverId, false));
            if (success) return Result.Success();

            await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "쪽지 전송에 실패하였습니다."));
            return Result.Failure(ErrorType.ProgramError);
        }
        catch (Exception exception)
        {
            await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"쪽지 전송에 실패하였습니다.\n{exception.Message}"));
            return Result.Failure(ErrorType.ProgramError);
        }
    }
}

// Outcome of a Kakao Story mirror attempt: the error message (when it failed) and the
// number of images skipped (format conversion failure or the image count limit).
public record KakaoStoryMirrorResult(bool IsSuccess, string ErrorMessage, int SkippedImageCount);