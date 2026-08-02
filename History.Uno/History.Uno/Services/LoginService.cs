using System.Text.Json;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Uno.DataTypes;
using History.Uno.Pages;
using Microsoft.UI.Xaml.Controls;
using Result = History.Commons.Result;

namespace History.Uno.Services;

/// <summary>
/// Shared login / registration business logic — platform-independent.
/// UI pages call into these static methods.
/// </summary>
public static class LoginService
{
    private static string s_appleUserFullName;

    /// <summary>
    /// Called after a successful OAuth login (Google or Apple).
    /// Fetches the user profile, refreshes friends/FCM token, and navigates to the main shell.
    /// </summary>
    public static async Task<Result> AfterLoginAsync()
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

            App.RootFrame.Navigate(typeof(MainPage));

            // Process pending push notification / shared media / shared text
            var pushData = Configuration.GetValue<string>("PushData");
            if (!string.IsNullOrEmpty(pushData)) await NotificationHandler.HandlePushNotificationAsync(pushData);

            // TODO: 3단계 EditPostPage 이전 후 활성화
            // var mediaData = Configuration.GetValue<string>("MediaData");
            // var sharedText = Configuration.GetValue<string>("SharedText");
        }
        else if (meResult.Error == ErrorType.Unauthorized) await App.DisplayAlertAsync("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.", Constants.PromptOk);

        return meResult;
    }

    public static async Task RefreshFriendsAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(Shared.UserId));
        if (friendsResult.IsSuccess) Shared.Friends = friendsResult.Value;
    }

    /// <summary>
    /// Performs the OAuth login flow: sends idToken to the server, stores tokens,
    /// and calls AfterLoginAsync.
    /// </summary>
    public static async Task<Result> LoginAsync(string idToken, SocialService socialService)
    {
        var result = await App.ExecuteRequestAsync(new Login(idToken, socialService), [ErrorType.NotFound, ErrorType.Forbidden]);
        if (result.IsSuccess)
        {
            var accessToken = result.Value.AccessToken;
            var refreshToken = result.Value.RefreshToken;

            Configuration.SetValue("AccessToken", accessToken);
            Configuration.SetValue("RefreshToken", refreshToken);

            Shared.ApiHandler = new(accessToken, refreshToken);
            return await AfterLoginAsync();
        }
        else if (result.Error == ErrorType.NotFound)
        {
            var willing = await App.DisplayAlertAsync("안내", "가입이 필요합니다. 가입하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (willing == ContentDialogResult.Primary) await App.PushAsync(typeof(RegisterPage), (idToken, socialService, s_appleUserFullName));
            else await App.DisplayAlertAsync("안내", "서비스 이용을 위해서는 가입이 필요합니다.", Constants.PromptOk);
        }
        else if (result.Error == ErrorType.Forbidden) await App.DisplayAlertAsync("안내", "서비스 이용이 제한되었습니다.", Constants.PromptOk);

        return result;
    }

    /// <summary>
    /// Apple Sign-In full name is captured during the native iOS auth flow
    /// and passed to RegisterPage if the user needs to register.
    /// </summary>
    public static void SetAppleUserFullName(string fullName) => s_appleUserFullName = fullName;

    /// <summary>
    /// Called by LoginPage.OnNavigatedTo when the AppleLoginPage modal returns a result.
    /// </summary>
    public static async Task LoginWithAppleResultAsync(OAuthRegisterRequestDto result)
    {
        if (result == null)
        {
            await App.DisplayAlertAsync("오류", "애플 로그인에 실패했습니다. 다시 시도해주세요.", Constants.PromptOk);
            return;
        }

        s_appleUserFullName = result.Name;
        await LoginAsync(result.IdToken, SocialService.Apple);
    }
}
