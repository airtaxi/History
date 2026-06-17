using System.Net;
using CommunityToolkit.Maui.Alerts;
using History.Commons;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class KakaoStoryLoginPage : ContentPage
{
    private readonly TaskCompletionSource<List<Cookie>> _taskCompletionSource = new();

    public KakaoStoryLoginPage() => InitializeComponent();

    public Task<List<Cookie>> GetResultAsync() => _taskCompletionSource.Task;

    private System.Timers.Timer _timer;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if(_timer == null)
        {
            _timer = new(1250);
            _timer.Elapsed += (s, e) => Dispatcher.Dispatch(async () => await CheckCookies());
            _timer.Start();
        }

        BrowserWebView.Source = "https://accounts.kakao.com/logout?continue=https%3A%2F%2Faccounts.kakao.com%2Flogin%2F%3Fcontinue%3Dhttps%253A%252F%252Fstory.kakao.com";
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

        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(null);

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
    }

    private async void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        MainActivityIndicator.IsVisible = false;
        MainActivityIndicator.IsRunning = false;

        await CheckCookies();
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

    private readonly SemaphoreSlim _checkCookiesSemaphore = new(1, 1);
    private bool _gotCookies;
    private async Task CheckCookies()
    {
        if (_gotCookies) return;
        if (!await _checkCookiesSemaphore.WaitAsync(0)) return;
        try
        {
            if (_gotCookies) return;

            var cookies = await WebViewCookieHelper.GetCookieListAsync(BrowserWebView, "https://story.kakao.com");
            if (cookies == null) return;

            bool isSuccess = cookies.Any(x => x.Name == "_kau");
            if (!isSuccess) return;

            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies) cookieContainer.Add(cookie);

            KakaoStoryApiHandler.Init(cookieContainer, cookies, null);

            await KakaoStoryApiHandler.GetFriends();

            _gotCookies = true;
            Configuration.SetValue("KakaoStoryCookies", cookies);
            _taskCompletionSource.TrySetResult(cookies);

            await App.PopModalAsync();
        }
        catch { }
        finally { _checkCookiesSemaphore.Release(); }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}