using CommunityToolkit.Maui.Alerts;
using History.Commons;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.Pages;

public partial class KakaoStoryLoginPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();

    private const string OAuthAuthorizeUrl = "https://kauth.kakao.com/oauth/authorize";
    private const string OAuthClientId = "2a8b2aa0dc2c4e9121bbd4b9bdb70bc1";
    private const string OAuthRedirectUri = "https://story.kakao.com/s/oauth";

    public KakaoStoryLoginPage() => InitializeComponent();

    public Task<bool> GetResultAsync() => _taskCompletionSource.Task;

    private System.Timers.Timer _timer;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_timer == null)
        {
            _timer = new(1250);
            _timer.Elapsed += (s, e) => Dispatcher.Dispatch(async () => await CheckLoginResult());
            _timer.Start();
        }

        // The kauth session silently issues an authorization code when the account
        // is still signed in; otherwise the user completes the login on the page.
        var state = Guid.NewGuid().ToString("N");
        BrowserWebView.Source = $"{OAuthAuthorizeUrl}?client_id={OAuthClientId}&redirect_uri={Uri.EscapeDataString(OAuthRedirectUri)}&response_type=code&state={state}";
        Toast.Make("간편 로그인은 지원하지 않습니다. 계정을 입력하여 로그인해주세요").Show();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(false);

        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
    }

    private void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        MainActivityIndicator.IsVisible = true;
        MainActivityIndicator.IsRunning = true;

        // The s/oauth callback page runs the web client's oauth.min.js, which
        // exchanges (and thereby consumes) the authorization code itself. Cancel
        // the navigation so the code survives for our kauth token exchange.
        if (e.Url?.StartsWith(OAuthRedirectUri) == true)
        {
            _currentUrl = e.Url;
            e.Cancel = true;
        }
    }

    private async void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        MainActivityIndicator.IsVisible = false;
        MainActivityIndicator.IsRunning = false;

        // iOS reports a cancelled navigation as a failure carrying the previous
        // page's URL (the kauth page), which would clobber the s/oauth URL captured
        // in OnNavigating. Keep the pending code URL so CheckLoginResult completes.
        if (_currentUrl?.StartsWith(OAuthRedirectUri) != true) _currentUrl = e.Url;

        await CheckLoginResult();
        await UncheckSaveSignedInAsync();
        await TryAutoFillCredentialsAsync();
    }

    private async Task UncheckSaveSignedInAsync()
    {
        var script = @"
            (function() {
                var saveSignedIn = document.querySelector('input[name=""saveSignedIn""]');
                if (saveSignedIn && saveSignedIn.checked) saveSignedIn.click();
            })();
        ";
        try { await BrowserWebView.EvaluateJavaScriptAsync(script); }
        catch { }
    }

    private async Task TryAutoFillCredentialsAsync()
    {
        var savedEmail = Configuration.GetValue<string>("KakaoStoryEmail");
        var savedEncryptedPassword = Configuration.GetValue<string>("KakaoStoryPassword");
        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedEncryptedPassword)) return;

        try
        {
            var password = AesCryptoHelper.Decrypt(savedEncryptedPassword, Constants.KakaoStoryCredentialEncryptionKey);

            await Task.Delay(500);

            var escapedEmail = savedEmail.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var escapedPassword = password.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var script = $@"
                (function tryFill(attempts) {{
                    var proto = Object.getOwnPropertyDescriptor(
                        window.HTMLInputElement.prototype, 'value');
                    var nativeSetter = proto && proto.set;

                    var emailInput = document.querySelector('input[name=""loginId""]');
                    var passInput = document.querySelector('input[name=""password""]');
                    var btn = document.querySelector('button.submit[type=""submit""]');

                    if (!nativeSetter || !emailInput || !passInput || !btn) {{
                        if (attempts > 0) setTimeout(function() {{ tryFill(attempts - 1); }}, 500);
                        return false;
                    }}

                    nativeSetter.call(emailInput, ""{escapedEmail}"");
                    emailInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    emailInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    nativeSetter.call(passInput, ""{escapedPassword}"");
                    passInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    passInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    setTimeout(function() {{ btn.click(); }}, 300);
                    return true;
                }})(5);
            ";

            await BrowserWebView.EvaluateJavaScriptAsync(script);
        }
        catch
        {
            await Toast.Make("저장된 로그인 정보 자동 입력에 실패하였습니다. 수동으로 로그인해주세요.").Show();
        }
    }

    private readonly SemaphoreSlim _checkLoginSemaphore = new(1, 1);
    private bool _gotLoginResult;
    private string _currentUrl;
    private async Task CheckLoginResult()
    {
        if (_gotLoginResult) return;
        if ((await _checkLoginSemaphore.WaitAsync(0)) == false) return;
        try
        {
            if (_gotLoginResult) return;

            var currentUrl = _currentUrl;
            if (currentUrl == null || !currentUrl.StartsWith(OAuthRedirectUri)) return;

            var query = currentUrl.Contains('?') ? currentUrl.Substring(currentUrl.IndexOf('?') + 1) : null;
            var code = query?.Split('&').FirstOrDefault(parameter => parameter.StartsWith("code="))?.Substring("code=".Length);
            if (string.IsNullOrEmpty(code)) return;

            var token = await KakaoStoryApiHandler.RefreshSdkTokenAsync(authorizationCode: code);
            if (token == null) return;

            KakaoStoryApiHandler.Init(null, null, null);

            Shared.KakaoFriends = (await KakaoStoryApiHandler.GetFriends())?.profiles;

            _gotLoginResult = true;
            _taskCompletionSource.TrySetResult(true);

            await App.PopModalAsync();
        }
        catch { }
        finally { _checkLoginSemaphore.Release(); }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}