using System.Net;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Models;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.WindowsClient.ViewModels;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;

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

    // Validates the saved SDK tokens (KAuth) and, when they are missing/expired,
    // presents the KakaoStoryLoginWindow. Returns true when a valid session is
    // available afterwards. When the session is already valid, the friends and
    // user-id caches are refreshed only when they are empty (cold start or an
    // earlier cache wipe); otherwise routine navigation costs no extra requests.
    public static async Task<bool> EnsureLoggedInAsync()
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

        return await ShowLoginModalAsync();
    }

    // Presents the auto-fill prompt (when no credential is saved) and the login window.
    // The login window refreshes friends and uploads the poll token on success.
    private static async Task<bool> ShowLoginModalAsync()
    {
        // 401 responses can arrive from background threads; the dialogs and the modal
        // window must run on the UI thread.
        var frame = MainWindow.Frame;
        if (!frame.DispatcherQueue.HasThreadAccess)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            frame.DispatcherQueue.TryEnqueue(async () => taskCompletionSource.TrySetResult(await ShowLoginModalOnUiThreadAsync(frame)));
            return await taskCompletionSource.Task;
        }

        return await ShowLoginModalOnUiThreadAsync(frame);
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
            if (mediaData == null && !string.IsNullOrWhiteSpace(externalUrl))
            {
                var scrapTryCount = 0;
                const int scrapMaxRetryCount = 5;
                async Task DoScrapAsync()
                {
                    try { scrap = await KakaoStoryApiHandler.GetScrapData(externalUrl); }
                    catch (WebException)
                    {
                        scrapTryCount++;
                        if (scrapTryCount >= scrapMaxRetryCount) throw;
                        else await DoScrapAsync();
                    }

                    if (!KakaoStoryApiHandler.IsScrapDataUsable(scrap))
                    {
                        scrapTryCount++;
                        if (scrapTryCount >= scrapMaxRetryCount) return;
                        else await DoScrapAsync();
                    }
                }
                await DoScrapAsync();
            }
            else if (mediaData != null && !string.IsNullOrWhiteSpace(externalUrl)) quoteDatas.Add(new QuoteData { type = "text", text = $"\n\n{externalUrl}" });

            await KakaoStoryApiHandler.WritePost(quoteDatas, mediaData, MapDiscoveryOptionToKakaoPermission(discoveryOption), true, !disallowShare, null, null, scrap);
            return new KakaoStoryMirrorResult(true, null, skippedImageCount);
        }
        catch (Exception exception) { return new KakaoStoryMirrorResult(false, GetMirrorErrorMessage(exception), skippedImageCount); }
    }

    public static bool IsKakaoStoryUnsupportedImageFormat(string fileName) =>
        fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".heif", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase);

    // Maps a History discovery option to the Kakao Story permission value. Only
    // Everyone/OnlyMe have exact Kakao Story equivalents; everything else is friends.
    private static string MapDiscoveryOptionToKakaoPermission(DiscoveryOption discoveryOption)
    {
        if (discoveryOption == DiscoveryOption.Everyone) return "A";
        else if (discoveryOption == DiscoveryOption.OnlyMe) return "M";
        else return "F";
    }

    // Builds the Kakao Story media caption payload from a media description. An empty
    // description maps to an empty caption array (verified web request format).
    private static List<MediaData.CaptionData> BuildKakaoStoryMediaCaption(string description) => string.IsNullOrEmpty(description) ? [] : [new MediaData.CaptionData { text = description }];

    private static void TryDeleteTempFile(string filePath)
    {
        try { File.Delete(filePath); }
        catch { }
    }

    // Reads the Kakao Story API error body (when the failure is an HTTP error) so the
    // user sees the server's reason instead of a generic message.
    private static string GetMirrorErrorMessage(Exception exception)
    {
        if (exception is not WebException { Response: HttpWebResponse response }) return exception.Message;

        try
        {
            using var responseReader = new StreamReader(response.GetResponseStream());
            return $"[{(int)response.StatusCode}] {responseReader.ReadToEnd()}";
        }
        catch { return exception.Message; }
    }
}

// Outcome of a Kakao Story mirror attempt: the error message (when it failed) and the
// number of images skipped (format conversion failure or the image count limit).
public record KakaoStoryMirrorResult(bool IsSuccess, string ErrorMessage, int SkippedImageCount);