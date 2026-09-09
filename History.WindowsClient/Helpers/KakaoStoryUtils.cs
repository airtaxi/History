using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Models;
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
}