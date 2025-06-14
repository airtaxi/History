using System.Net;
using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class KakaoStoryLoginPage : ContentPage
{
    private readonly TaskCompletionSource<List<Cookie>> _taskCompletionSource = new();

    public KakaoStoryLoginPage() => InitializeComponent();

    public Task<List<Cookie>> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrowserWebView.Source = "https://accounts.kakao.com/login/?continue=https%3A%2F%2Fstory.kakao.com%2F&talk_login=&login_type=simple#login";
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
            var setResult = _taskCompletionSource.TrySetResult(cookies);
            if (setResult) await App.PopModalAsync();
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}