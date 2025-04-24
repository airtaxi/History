using History.Commons;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.Auth;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

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

        try
        {
            IsEnabled = false;
            IsBusy = true;
            var me = await Shared.ApiHandler.ExecuteRequestAsync(new GetMyProfile());
            Shared.UserId = me.UserId;
            Shared.LastUsedPostDiscoveryOption = me.LastUsedPostDiscoveryOption;
            App.MainWindow.Page = new MainPage();
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Unauthorized) await DisplayAlert("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.", "확인");
            else await DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n코드: {exception.Message}", "확인");
        }
        finally
        {
            IsEnabled = true;
            IsBusy = false;
        }
    }

    private async Task Login(string idToken, SocialService socialService)
    {
        try
        {
            var response = await ApiHandler.Public.ExecuteRequestAsync(new Login(idToken, socialService));
            var accessToken = response.AccessToken;
            var refreshToken = response.RefreshToken;

            Configuration.SetValue("AccessToken", accessToken);
            Configuration.SetValue("RefreshToken", refreshToken);

            Shared.ApiHandler = new(accessToken, refreshToken);
            await AfterLogin();
        }
        catch (HttpRequestException loginException)
        {
            if (loginException.StatusCode == HttpStatusCode.NotFound)
            {
                var willing = await DisplayAlert("안내", "가입 신청이 필요합니다. 가입하시겠습니까?", "예", "아니요");
                if (willing)
                {
                    try
                    {
                        await ApiHandler.Public.ExecuteRequestAsync(new Register(idToken, SocialService.Google));
                        await DisplayAlert("안내", "가입 신청이 완료되었습니다. 심사가 완료되는대로 가입이 완료됩니다.", "확인");
                    }
                    catch (HttpRequestException registerException)
                    {
                        await DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n코드: {registerException.Message}", "확인");
                    }
                }
                else
                {
                    await DisplayAlert("안내", "가입 신청이 필요합니다.", "확인");
                }
            }
            else if (loginException.StatusCode == HttpStatusCode.Forbidden)
            {
                await DisplayAlert("안내", "가입 신청 대기중입니다. 심사가 완료되는대로 가입이 완료됩니다.", "확인");
            }
            else
            {
                await DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n코드: {loginException.Message}", "확인");
            }
        }
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
        await DisplayAlert("안내", "구현되지 않은 플랫폼입니다.", "확인");
#endif
    }

    private async void OnAppleLoginButtonClicked(object sender, EventArgs e) => await DisplayAlert("안내", "애플 로그인은 개발 중입니다.", "확인");

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