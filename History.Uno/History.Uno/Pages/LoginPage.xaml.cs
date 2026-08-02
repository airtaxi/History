using History.Commons.Enums;
using History.Uno.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.Uno.Pages;

/// <summary>
/// Login page — UI only. All business logic is in LoginService.
/// </summary>
public sealed partial class LoginPage : Page
{
    private bool _isInForeground;

    public LoginPage()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isInForeground = true;

        // Try auto-login with stored tokens
        var accessToken = Configuration.GetValue<string>("AccessToken");
        var refreshToken = Configuration.GetValue<string>("RefreshToken");

        if (accessToken != null && refreshToken != null)
        {
            Shared.ApiHandler = new(accessToken, refreshToken);
            var result = await LoginService.AfterLoginAsync();
            if (result.IsFailure) LoginStackPanel.Visibility = Visibility.Visible;
        }
        else LoginStackPanel.Visibility = Visibility.Visible;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _isInForeground = false;
    }

    private async void OnGoogleLoginButtonClicked(object sender, RoutedEventArgs e)
    {
#if __ANDROID__
        var service = new GoogleAuthService();
        var idToken = await service.AuthenticateAsync();
        if (idToken != null)
        {
            await service.SignOutAsync();
            await LoginService.LoginAsync(idToken, SocialService.Google);
        }
        else await App.DisplayAlertAsync("오류", "idToken이 존재하지 않습니다");
#elif __IOS__
        var service = new GoogleAuthService();
        var idToken = await service.AuthenticateAsync();
        if (idToken != null) await LoginService.LoginAsync(idToken, SocialService.Google);
#endif
    }

    private async void OnAppleLoginButtonClicked(object sender, RoutedEventArgs e)
    {
#if __IOS__
        // Native Apple Sign-In via AuthenticationServices
        var service = new AppleSignInService();
        var result = await service.AuthenticateAsync();
        if (result?.IdToken != null)
        {
            LoginService.SetAppleUserFullName(result.FullName);
            await LoginService.LoginAsync(result.IdToken, SocialService.Apple);
        }
#else
        // Android: WebView-based Apple Sign-In
        await App.PushAsync(typeof(AppleLoginPage));
#endif
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        {
            MainProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = !isLoading;
        });
    }
}
