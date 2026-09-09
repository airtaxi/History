using History.Commons;
using History.Commons.KakaoStory;

namespace History.WindowsClient.ViewModels;

public partial class KakaoStoryLoginWindowViewModel : BaseViewModel
{
    // OAuth credentials for the story.kakao.com web application; the client id
    // authorizes the authorization_code flow on kauth.kakao.com.
    private const string OAuthAuthorizeUrl = "https://kauth.kakao.com/oauth/authorize";
    private const string OAuthClientId = "2a8b2aa0dc2c4e9121bbd4b9bdb70bc1";
    public const string OAuthRedirectUri = "https://story.kakao.com/s/oauth";

    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();
    private readonly SemaphoreSlim _checkLoginSemaphore = new(1, 1);
    private bool _gotLoginResult;

    public Task<bool> GetResultAsync() => _taskCompletionSource.Task;

    // Builds the kauth authorize URL with a fresh state parameter for the OAuth round trip.
    public string BuildAuthorizeUrl()
    {
        var state = Guid.NewGuid().ToString("N");
        return $"{OAuthAuthorizeUrl}?client_id={OAuthClientId}&redirect_uri={Uri.EscapeDataString(OAuthRedirectUri)}&response_type=code&state={state}";
    }

    // Exchanges the authorization code captured from the s/oauth callback for SDK
    // tokens and warms up the session caches. Called repeatedly (polling and navigation
    // events), so the semaphore and the completed flag keep it idempotent. Returns
    // whether the login completed on this call.
    public async Task<bool> TryCompleteLoginAsync(string currentUrl)
    {
        if (_gotLoginResult) return false;
        if ((await _checkLoginSemaphore.WaitAsync(0)) == false) return false;
        try
        {
            if (_gotLoginResult) return false;

            if (currentUrl == null || !currentUrl.StartsWith(OAuthRedirectUri)) return false;

            var query = currentUrl.Contains('?') ? currentUrl.Substring(currentUrl.IndexOf('?') + 1) : null;
            var code = query?.Split('&').FirstOrDefault(parameter => parameter.StartsWith("code="))?.Substring("code=".Length);
            if (string.IsNullOrEmpty(code)) return false;

            var token = await KakaoStoryApiHandler.RefreshSdkTokenAsync(authorizationCode: code);
            if (token == null) return false;

            KakaoStoryApiHandler.Init(null, null, null);

            CommonShared.KakaoFriends = (await KakaoStoryApiHandler.GetFriends())?.profiles;

            _gotLoginResult = true;
            _taskCompletionSource.TrySetResult(true);

            // Register the server session immediately so notifications start flowing
            // without waiting for the next background refresh.
            await CommonKakaoStoryUtils.UploadTokenToServerAsync();

            return true;
        }
        catch { return false; }
        finally { _checkLoginSemaphore.Release(); }
    }

    // Completes the pending result as a cancel when the window closes without a login.
    public void CompleteAsCanceled()
    {
        if (!_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.TrySetResult(false);
        }
    }
}