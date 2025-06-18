using System.Net;
using System.Text.Json.Nodes;
using System.Web;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using Newtonsoft.Json;

namespace History.MobileClient.Pages;

public partial class AppleLoginPage : ContentPage
{
    private readonly TaskCompletionSource<OAuthRegisterRequestDto> _taskCompletionSource = new();

    public AppleLoginPage() => InitializeComponent();

    public Task<OAuthRegisterRequestDto> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrowserWebView.Source = "https://api.history.cenox.io/api/auth/apple/login?redirectUrl=http://localhost/";
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
                var result = new OAuthRegisterRequestDto
                {
                    IdToken = idToken,
                    Provider = SocialService.Apple
                };

                var userJson = queryParams["user"];
                if (userJson != null)
                {
                    var user = JsonNode.Parse(userJson);
                    var name = user["name"]?.AsObject();
                    // Korean names are typically in the format "성(Last Name) + 이름(First Name)"
                    if (name != null) result.Name = name["lastName"]?.ToString() + name["firstName"]?.ToString();
                }

                _taskCompletionSource.TrySetResult(result);
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