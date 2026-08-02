using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;

namespace History.Uno.Pages;

/// <summary>
/// WebView2-based Apple Sign-In for Android (and iOS < 13).
/// The server handles the Apple OAuth flow and redirects to localhost with
/// id_token and user info in the query string.
/// </summary>
public sealed partial class AppleLoginPage : Page
{
    private readonly TaskCompletionSource<OAuthRegisterRequestDto> _taskCompletionSource = new();

    public AppleLoginPage()
    {
        InitializeComponent();
    }


    public Task<OAuthRegisterRequestDto> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWebView.Source = new Uri("https://api.history.cenox.io/api/auth/apple/login?redirectUrl=http://localhost/");
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(null);
    }


    private async void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (args.Uri == null) return;

        if (args.Uri.StartsWith("http://localhost"))
        {
            args.Cancel = true;

            var uri = new Uri(args.Uri);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);

            var idToken = queryParams["id_token"];
            if (idToken == null)
            {
                _taskCompletionSource.TrySetResult(null);
                await App.PopAsync();
                return;
            }

            var result = new OAuthRegisterRequestDto
            {
                IdToken = idToken,
                Provider = SocialService.Apple
            };

            var userJson = queryParams["user"];
            if (userJson != null)
            {
                var user = JsonNode.Parse(userJson);
                var name = user?["name"]?.AsObject();
                if (name != null) result.Name = name["lastName"]?.ToString() + name["firstName"]?.ToString();
            }

            _taskCompletionSource.TrySetResult(result);
            await App.PopAsync();
        }
    }

    private async void OnBackButtonClicked(object sender, RoutedEventArgs e) => await App.PopAsync();
}
