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