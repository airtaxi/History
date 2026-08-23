using System.Web;

namespace History.MobileClient.Pages;

public partial class GoogleLoginPage : ContentPage
{
    private readonly TaskCompletionSource<string> _taskCompletionSource = new();

    public GoogleLoginPage() => InitializeComponent();

    public Task<string> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrowserWebView.Source = "https://api.history.cenox.io/api/auth/google/login?redirectUrl=http://localhost/";
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(null);
    }

    private async void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        MainActivityIndicator.IsVisible = true;
        MainActivityIndicator.IsRunning = true;
        if (e.Url.StartsWith("http://localhost"))
        {
            var uri = new Uri(e.Url);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);

            var idToken = queryParams["id_token"];
            if (idToken == null)
            {
                _taskCompletionSource.TrySetResult(null);
                await App.PopModalAsync();
            }
            else
            {
                _taskCompletionSource.TrySetResult(idToken);
                await App.PopModalAsync();
            }
        }
    }

    private void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        MainActivityIndicator.IsVisible = false;
        MainActivityIndicator.IsRunning = false;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}
