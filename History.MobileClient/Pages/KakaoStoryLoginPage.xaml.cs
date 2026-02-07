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
        await TryAutoFillCredentialsAsync();
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
                (function() {{
                    var nativeSetter = Object.getOwnPropertyDescriptor(
                        window.HTMLInputElement.prototype, 'value').set;

                    var emailInput = document.querySelector('input[name=""loginId""]');
                    if (!emailInput) return false;

                    var passInput = document.querySelector('input[name=""password""]');
                    if (!passInput) return false;

                    var btn = document.querySelector('button.submit[type=""submit""]');
                    if (!btn) return false;

                    emailInput.focus();
                    nativeSetter.call(emailInput, ""{escapedEmail}"");
                    emailInput.dispatchEvent(new Event('input',  {{ bubbles: true }}));
                    emailInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    passInput.focus();
                    nativeSetter.call(passInput, ""{escapedPassword}"");
                    passInput.dispatchEvent(new Event('input',  {{ bubbles: true }}));
                    passInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    var saveSignedIn = document.querySelector('input#saveSignedIn--4[name=""saveSignedIn""]');
                    if (saveSignedIn && saveSignedIn.checked) {{
                        saveSignedIn.click();
                    }}

                    btn.click();

                    return true;
                }})();
            ";

            await BrowserWebView.EvaluateJavaScriptAsync(script);
        }
        catch
        {
            await Toast.Make("저장된 로그인 정보 자동 입력에 실패하였습니다. 수동으로 로그인해주세요.").Show();
        }
    }

    private bool _gotCookies = false;
    private async Task CheckCookies()
    {
        var cookies = await WebViewCookieHelper.GetCookieListAsync(BrowserWebView, "https://story.kakao.com");
        if (cookies == null) return;

        if (_gotCookies) return;

        bool isSuccess = cookies.Any(x => x.Name == "_karmt");
        if (isSuccess)
        {
            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies) cookieContainer.Add(cookie);

            KakaoStoryApiHandler.Init(cookieContainer, cookies, null);
            try
            {
                var friends = await KakaoStoryApiHandler.GetFriends();

                _gotCookies = true; // Successfully got cookies, prevent further checks

                Configuration.SetValue("KakaoStoryCookies", cookies);
                _taskCompletionSource.TrySetResult(cookies);

                await App.PopModalAsync();
            }
            catch { }
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}