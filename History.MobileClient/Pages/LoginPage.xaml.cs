using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.Auth;
using History.MobileClient.DataTypes;
using Result = History.Commons.Result;

#if ANDROID
using Android.Content;
#endif

namespace History.MobileClient.Pages;

public partial class LoginPage : ContentPage
{
    private bool _isInForeground;
    private static string s_appleUserFullName;

    public LoginPage()
	{
		InitializeComponent();

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
        }
        else if (meResult.Error == ErrorType.Unauthorized) await App.Page.DisplayAlert("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.", Constants.PromptOk);

        return meResult;
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
            var willing = await App.Page.DisplayAlert("안내", "가입이 필요합니다. 가입하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (willing) await App.PushAsync(new RegisterPage(idToken, socialService, s_appleUserFullName));
            else await App.Page.DisplayAlert("안내", "서비스 이용을 위해서는 가입이 필요합니다.", Constants.PromptOk);
        }
        else if (result.Error == ErrorType.Forbidden) await App.Page.DisplayAlert("안내", "서비스 이용이 제한되었습니다.", Constants.PromptOk);

        return result;
    }

    private async void OnGoogleLoginButtonClicked(object sender, EventArgs e)
    {
#if ANDROID || IOS
        var service = new GoogleAuthService();
        var idToken = await service.AuthenticateAsync();
        if (idToken != null)
        {
            await service.SignOutAsync();
            await Login(idToken, SocialService.Google);
        }
#else
        await DisplayAlert("안내", "구현되지 않은 플랫폼입니다.", Constants.PromptOk);
#endif
    }

    private async void OnAppleLoginButtonClicked(object sender, EventArgs e)
    {
        if(DeviceInfo.Platform == DevicePlatform.iOS && DeviceInfo.Version.Major >= 13)
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
        else await DisplayAlert("안내", "애플 로그인은 개발 중입니다.", Constants.PromptOk);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        try
        {
#if ANDROID
            var versionUrl = "https://kagamine-rin.com/History/version_android";
#else
            var versionUrl = "https://kagamine-rin.com/History/version_ios";
#endif
            var remoteVersionString = await Downloader.DownloadString(versionUrl);
            var localVersionString = AppInfo.Current.VersionString;

            var remoteVersion = Version.Parse(remoteVersionString);
            var localVersion = Version.Parse(localVersionString);
            if (remoteVersion <= localVersion)
            {
                await Toast.Make("최신 버전을 사용중입니다.").Show();
                return;
            }
#if ANDROID
            var shouldDownload = await DisplayAlert("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})\n업데이트 하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!shouldDownload) return;
            var downloadUrl = "https://kagamine-rin.com/History/com.airtaxi.history-Signed.apk";
            var apkFilePath = Path.Combine(FileSystem.CacheDirectory, "History.apk");

            await Toast.Make("업데이트를 다운로드 중입니다. 잠시만 기다려 주세요.").Show();
            await Downloader.DownloadFileAsync(downloadUrl, apkFilePath);

            var context = Platform.CurrentActivity ?? Android.App.Application.Context;

            var file = new Java.IO.File(apkFilePath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);

#pragma warning disable CA1422 // Validate platform compatibility
            var intent = new Intent(Intent.ActionInstallPackage);
#pragma warning restore CA1422 // Validate platform compatibility
            intent.SetData(uri);
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            context.StartActivity(intent);
#else
            await DisplayAlert("업데이트 알림", $"새로운 버전이 있습니다. ({localVersionString} → {remoteVersionString})", Constants.PromptOk);
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"History Update Error: {ex.Message}");
            await DisplayAlert("오류", $"업데이트 중 문제가 발생했습니다: {ex.Message}", "확인");
        }
        finally
        {
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }
}