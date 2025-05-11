using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.PushNotification;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.Auth;
using Plugin.Firebase.CloudMessaging;
using System.Text.Json;

namespace History.MobileClient.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

    private async Task AfterLogin()
    {
        if (Shared.ApiHandler == null) return;

        var meResult = await App.ExecuteRequestAsync(new GetMyProfile(), [ErrorType.Unauthorized]);
        if (meResult.IsSuccess)
        {
            var me = meResult.Value;
            Shared.UserId = me.UserId;
            Shared.LastUsedPostDiscoveryOption = me.LastUsedPostDiscoveryOption;
            
            await RefreshFriends();

            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            var firebaseToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            await App.ExecuteRequestAsync(new RegisterFirebaseToken(firebaseToken));

            App.Page = new AppShell();

            var pushData = Preferences.Get("PushData", null);
            if (!string.IsNullOrEmpty(pushData)) await App.HandlePushNotificationAsync(pushData);
        }
        else if (meResult.Error == ErrorType.Unauthorized) await DisplayAlert("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.", Constants.PromptOk);
        else await DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n코드: {meResult.ErrorMessage}", Constants.PromptOk);
    }

    public static async Task RefreshFriends()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(Shared.UserId));
        Shared.Friends = friendsResult.Value;
    }

    private async Task Login(string idToken, SocialService socialService)
    {
        var result = await App.ExecuteRequestAsync(new Login(idToken, socialService), [ErrorType.NotFound, ErrorType.Forbidden]);
        if (result.IsSuccess)
        {
            var accessToken = result.Value.AccessToken;
            var refreshToken = result.Value.RefreshToken;

            Configuration.SetValue("AccessToken", accessToken);
            Configuration.SetValue("RefreshToken", refreshToken);

            Shared.ApiHandler = new(accessToken, refreshToken);
            await AfterLogin();
        }
        else if (result.Error == ErrorType.NotFound)
        {
            var willing = await DisplayAlert("안내", "가입 신청이 필요합니다. 가입하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (willing)
            {
                result = await App.ExecuteRequestAsync(new Register(idToken, SocialService.Google));
                if(result.IsSuccess) await DisplayAlert("안내", "가입 신청이 완료되었습니다. 심사가 완료되는대로 가입이 완료됩니다.", Constants.PromptOk);
            }
            else await DisplayAlert("안내", "서비스 이용을 위해서는 가입 신청이 필요합니다.", Constants.PromptOk);
        }
        else if (result.Error == ErrorType.Forbidden) await DisplayAlert("안내", "가입 신청 대기중입니다. 심사가 완료되는대로 가입이 완료됩니다.", Constants.PromptOk);
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

    private async void OnAppleLoginButtonClicked(object sender, EventArgs e) => await DisplayAlert("안내", "애플 로그인은 개발 중입니다.", Constants.PromptOk);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var accessToken = Configuration.GetValue<string>("AccessToken");
        var refreshToken = Configuration.GetValue<string>("RefreshToken");

        if (accessToken != null && refreshToken != null)
        {
            Shared.ApiHandler = new(accessToken, refreshToken);
            await AfterLogin();
        }
    }
}