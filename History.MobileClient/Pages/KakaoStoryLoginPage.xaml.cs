using System.Net;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class KakaoStoryLoginPage : ContentPage
{
    private readonly TaskCompletionSource<List<Cookie>> _taskCompletionSource = new();

    public KakaoStoryLoginPage() => InitializeComponent();

    public Task<List<Cookie>> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrowserWebView.Source = "https://accounts.kakao.com/logout?continue=https://story.kakao.com/";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.TrySetResult(null);
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

        var cookies = await WebViewCookieHelper.GetCookieListAsync(BrowserWebView, "https://story.kakao.com");
        if (cookies == null) return;

        bool isSuccess = cookies.Any(x => x.Name == "_karmt");
        if (isSuccess)
        {
            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies) cookieContainer.Add(cookie);

            KakaoStoryApiHandler.Init(cookieContainer, cookies, null);
            try
            {
                var friends = await KakaoStoryApiHandler.GetFriends();
                var setResult = _taskCompletionSource.TrySetResult(cookies);
                if (setResult) await App.PopModalAsync();
            }
            catch { }

        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}