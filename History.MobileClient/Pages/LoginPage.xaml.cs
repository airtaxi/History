using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.Auth;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using Result = History.Commons.Result;

namespace History.MobileClient.Pages;

public partial class LoginPage : ContentPage
{
    private bool _isInForeground;
    private static string s_appleUserFullName;

    public LoginPage()
	{
		InitializeComponent();
        AppShell.IsLoaded = false;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private static async Task<Result> AfterLogin()
    {
        if (Shared.ApiHandler == null) return (ErrorType.Unauthorized, "API 핸들러가 초기화되지 않았습니다.");

        var meResult = await App.ExecuteRequestAsync(new GetMyProfile(), [ErrorType.Unauthorized]);
        if (meResult.IsSuccess)
        {
            var me = meResult.Value;
            Shared.UserId = me.UserId;
            Shared.MyRank = me.Rank;
            Shared.LastUsedPostDiscoveryOption = me.LastUsedPostDiscoveryOption;

            await RefreshFriendsAsync();
            await Utils.RefreshFirebaseToken();

#if ANDROID
            App.Page = new AppShell();
#else
            App.Page = new AppShell();
#endif

            App.Page.Dispatcher.Dispatch(async () =>
            {
                var pushData = Preferences.Get("PushData", null);
                if (!string.IsNullOrEmpty(pushData)) await App.HandlePushNotificationAsync(pushData);

                // Replay a Kakao Story notification tapped during a cold start,
                // deferred until the app shell was up.
                App.ReplayPendingKakaoStoryScheme();

                var mediaData = Preferences.Get("MediaData", null);
                if (!string.IsNullOrEmpty(mediaData))
                {
                    Preferences.Set("MediaData", null);
                    var mediaFiles = JsonSerializer.Deserialize<List<MediaFile>>(mediaData);
                    var page = new EditPostPage(mediaFiles);
                    await App.PushAsync(page);
                }

                var sharedText = Preferences.Get("SharedText", null);
                if (!string.IsNullOrEmpty(sharedText))
                {
                    Preferences.Set("SharedText", null);
                    var page = new EditPostPage(sharedText);
                    await App.PushAsync(page);
                }
            });
        }
        else if (meResult.Error == ErrorType.Unauthorized) await App.Page.DisplayAlertAsync("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.", Constants.PromptOk);

        return meResult;
    }

    public static DateTime s_lastBackPressedTime = DateTime.MinValue;
    protected override bool OnBackButtonPressed()
    {
        TimeSpan timeSinceLastBackPressed = DateTime.UtcNow - s_lastBackPressedTime;
        if (timeSinceLastBackPressed.TotalMilliseconds > 2000)
        {
            s_lastBackPressedTime = DateTime.UtcNow;
            Toast.Make("나가려면 한번 더 누르세요").Show();
        }
        else Environment.Exit(0);
        return true;
    }

    public static async Task RefreshFriendsAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(Shared.UserId));
        Shared.Friends = friendsResult.Value;
    }

    public static async Task<Result> Login(string idToken, SocialService socialService)
    {
        var result = await App.ExecuteRequestAsync(new Login(idToken, socialService), [ErrorType.NotFound, ErrorType.Forbidden]);
        if (result.IsSuccess)
        {
            var accessToken = result.Value.AccessToken;
            var refreshToken = result.Value.RefreshToken;

            Configuration.SetValue("AccessToken", accessToken);
            Configuration.SetValue("RefreshToken", refreshToken);

            Shared.ApiHandler = new(accessToken, refreshToken);
            return await AfterLogin();
        }
        else if (result.Error == ErrorType.NotFound)
        {
#if IOS
            await App.PushAsync(new RegisterPage(idToken, socialService, s_appleUserFullName));
#else
            var willing = await App.Page.DisplayAlertAsync("안내", "가입이 필요합니다. 가입하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (willing) await App.PushAsync(new RegisterPage(idToken, socialService, s_appleUserFullName));
            else await App.Page.DisplayAlertAsync("안내", "서비스 이용을 위해서는 가입이 필요합니다.", Constants.PromptOk);
#endif
        }
        else if (result.Error == ErrorType.Forbidden) await App.Page.DisplayAlertAsync("안내", "서비스 이용이 제한되었습니다.", Constants.PromptOk);
        else await App.Page.DisplayAlertAsync("안내", $"알 수 없는 오류가 발생했습니다: {result.Error}/{result.ErrorMessage}", Constants.PromptOk);

        return result;
    }

    private async void OnGoogleLoginButtonClicked(object sender, EventArgs e)
    {
        try
        {
            var service = new GoogleAuthService();
            var idToken = await service.AuthenticateAsync();
            if (idToken == null)
            {
                await DisplayAlertAsync("오류", "구글 로그인에 실패했습니다. 다시 시도해주세요.", Constants.PromptOk);
                return;
            }

            await service.SignOutAsync();
            await Login(idToken, SocialService.Google);
        }
        catch (Exception exception) { Debug.WriteLine($"Google login failed: {exception.Message}"); }
    }

    private async void OnAppleLoginButtonClicked(object sender, EventArgs e)
    {
        if (DeviceInfo.Platform == DevicePlatform.iOS && DeviceInfo.Version.Major >= 13)
        {
            var result = await AppleSignInAuthenticator.AuthenticateAsync(new AppleSignInAuthenticator.Options
            {
                IncludeFullNameScope = true,
                IncludeEmailScope = true
            });
            var idToken = result?.IdToken;
            if (idToken == null) return;

            result.Properties.TryGetValue("name", out s_appleUserFullName);

            await Login(idToken, SocialService.Apple);
        }
        else
        {
            var page = new AppleLoginPage();
            await App.PushModalAsync(page);

            var result = await page.GetResultAsync();
            if (result == null)
            {
                await DisplayAlertAsync("오류", "애플 로그인에 실패했습니다. 다시 시도해주세요.", Constants.PromptOk);
                return;
            }

            s_appleUserFullName = result.Name;
            await Login(result.IdToken, SocialService.Apple);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        await Utils.CheckForUpdateAsync();

        var accessToken = Configuration.GetValue<string>("AccessToken");
        var refreshToken = Configuration.GetValue<string>("RefreshToken");

        if (accessToken != null && refreshToken != null)
        {
            Shared.ApiHandler = new(accessToken, refreshToken);
            var result = await AfterLogin();
            if (result.IsFailure) LoginVerticalStackLayout.IsVisible = true;
        }
        else LoginVerticalStackLayout.IsVisible = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }
}