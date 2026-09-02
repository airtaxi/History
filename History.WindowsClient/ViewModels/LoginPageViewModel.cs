using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Services;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace History.WindowsClient.ViewModels;

public partial class LoginPageViewModel : BaseViewModel
{
    private const string GoogleLoginUrl = "https://api.history.cenox.io/api/auth/google/login?redirectUrl=history-app://auth/google";
    private const string AppleLoginUrl = "https://api.history.cenox.io/api/auth/apple/login?redirectUrl=history-app://auth/apple";
    private static readonly TimeSpan OAuthTimeout = TimeSpan.FromMinutes(5);

    private readonly ApplicationSettingsService _settingsService;
    private TaskCompletionSource<OAuthLoginMessage> _pendingOAuthTaskCompletionSource;

    [ObservableProperty]
    public partial Visibility LoginPanelVisibility { get; set; }

    public LoginPageViewModel(ApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;

        WeakReferenceMessenger.Default.Register<OAuthLoginMessage>(this, OnOAuthLoginMessageReceived);

        var accessToken = settingsService.Settings.AccessToken;
        var refreshToken = settingsService.Settings.RefreshToken;
        if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken)) LoginPanelVisibility = Visibility.Collapsed;
        else LoginPanelVisibility = Visibility.Visible;
    }

    private static string BuildLoginUrl(SocialService provider) => provider switch
    {
        SocialService.Google => GoogleLoginUrl,
        SocialService.Apple => AppleLoginUrl,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    private static string ExtractNameFromUserJson(string userJson)
    {
        if (string.IsNullOrEmpty(userJson)) return null;

        var user = JsonNode.Parse(userJson);
        var name = user?["name"]?.AsObject();
        if (name == null) return null;

        // Korean names are typically in the format "성(Last Name) + 이름(First Name)"
        return name["lastName"]?.ToString() + name["firstName"]?.ToString();
    }

    [RelayCommand]
    private async Task LoginGoogleAsync() => await StartOAuthFlowAsync(SocialService.Google);

    [RelayCommand]
    private async Task LoginAppleAsync() => await StartOAuthFlowAsync(SocialService.Apple);

    private async Task StartOAuthFlowAsync(SocialService provider)
    {
        if (_pendingOAuthTaskCompletionSource != null) return;

        OAuthLoginMessage message = null;
        await ExecuteWithLoadingAsync(async () =>
        {
            var taskCompletionSource = new TaskCompletionSource<OAuthLoginMessage>();
            _pendingOAuthTaskCompletionSource = taskCompletionSource;

            Process.Start(new ProcessStartInfo() { FileName = BuildLoginUrl(provider), UseShellExecute = true });

            var completedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(OAuthTimeout));
            message = completedTask == taskCompletionSource.Task ? await taskCompletionSource.Task : null;

            _pendingOAuthTaskCompletionSource = null;
        }, "브라우저 로그인 대기중...");

        if (message != null)
        {
            await LoginWithIdTokenAsync(message.IdToken, message.Provider, message.UserJson);
            return;
        }

        LoginPanelVisibility = Visibility.Visible;

        var timeoutDialogParameters = new MessageDialogParameters("오류", "브라우저 로그인이 완료되지 않았습니다. 다시 시도해주세요.");
        await ShowMessageDialogAsync(timeoutDialogParameters);
    }

    private void OnOAuthLoginMessageReceived(object recipient, OAuthLoginMessage message)
    {
        MainWindow.SetForegroundWindow();

        var taskCompletionSource = _pendingOAuthTaskCompletionSource;
        if (taskCompletionSource != null)
        {
            taskCompletionSource.TrySetResult(message);
            return;
        }

        // Activation without a matching interactive flow (e.g. cold start via
        // "history-app://" protocol): complete the login directly.
        _ = LoginWithIdTokenAsync(message.IdToken, message.Provider, message.UserJson);
    }

    // Called by LoginPage.OnNavigatedTo after the base page subscribed the dialog
    // and loading events, so every request is fulfilled by the page/window.
    public async Task TryAutoLoginAsync()
    {
        var accessToken = _settingsService.Settings.AccessToken;
        var refreshToken = _settingsService.Settings.RefreshToken;
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            LoginPanelVisibility = Visibility.Visible;
            return;
        }

        CommonShared.ApiHandler = new(accessToken, refreshToken);

        var profileResult = await GetMyProfileAsync(ErrorType.Unauthorized);
        if (profileResult.IsSuccess)
        {
            await LoadMyProfileAsync(this);
            NavigateToMainPage(this);
            return;
        }

        if (profileResult.Error == ErrorType.Unauthorized)
        {
            var expiredDialogParameters = new MessageDialogParameters("안내", "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.");
            await ShowMessageDialogAsync(expiredDialogParameters);
        }
        LoginPanelVisibility = Visibility.Visible;
    }

    private async Task LoginWithIdTokenAsync(string idToken, SocialService provider, string userJson = null)
    {
        var loginResult = await LoginRequestAsync(idToken, provider);
        if (loginResult.IsSuccess)
        {
            await LoadMyProfileAsync(this);
            NavigateToMainPage(this);
            return;
        }

        if (loginResult.Error == ErrorType.NotFound)
        {
            var notFoundDialogParameters = new MessageDialogParameters("안내", "가입이 필요합니다. 서비스 이용을 위해서는 가입이 필요합니다.");
            await ShowMessageDialogAsync(notFoundDialogParameters);
            ShowRegisterPage(idToken, provider, userJson);
        }
        else if (loginResult.Error == ErrorType.Forbidden)
        {
            var forbiddenDialogParameters = new MessageDialogParameters("안내", "서비스 이용이 제한되었습니다.");
            await ShowMessageDialogAsync(forbiddenDialogParameters);
        }
        else
        {
            var unknownErrorDialogParameters = new MessageDialogParameters("오류", $"알 수 없는 오류가 발생했습니다: {loginResult.Error}/{loginResult.ErrorMessage}");
            await ShowMessageDialogAsync(unknownErrorDialogParameters);
        }

        LoginPanelVisibility = Visibility.Visible;
    }

    private async Task<Result<OAuthLoginResponseDto>> LoginRequestAsync(string idToken, SocialService provider)
    {
        // Loading state and generic error handling are managed by the request wrapper.
        var loginResult = await ExecuteRequestAsync(new Login(idToken, provider), [ErrorType.NotFound, ErrorType.Forbidden]);
        if (loginResult.IsFailure) return loginResult;

        CommonShared.ApiHandler = new(loginResult.Value.AccessToken, loginResult.Value.RefreshToken);
        _settingsService.Settings.AccessToken = loginResult.Value.AccessToken;
        _settingsService.Settings.RefreshToken = loginResult.Value.RefreshToken;

        return loginResult;
    }

    private async Task<Result<UserResponseDto>> GetMyProfileAsync(params ErrorType[] hiddenErrorTypes) => await ExecuteRequestAsync(new GetMyProfile(), hiddenErrorTypes);

    public static async Task LoadMyProfileAsync(BaseViewModel baseViewModel)
    {
        var profileResult = await baseViewModel.ExecuteRequestAsync(new GetMyProfile());
        if (profileResult.IsSuccess)
        {
            CommonShared.UserId = profileResult.Value.UserId;
            CommonShared.MyRank = profileResult.Value.Rank;
            CommonShared.LastUsedPostDiscoveryOption = profileResult.Value.LastUsedPostDiscoveryOption;

            var friendsResult = await baseViewModel.ExecuteRequestAsync(new GetFriends(profileResult.Value.UserId));
            if (friendsResult.IsSuccess) CommonShared.Friends = friendsResult.Value;
        }
    }

    public static void NavigateToMainPage(BaseViewModel baseViewModel)
    {
        baseViewModel.HideLoading();
        MainWindow.Frame.Navigate(typeof(Pages.MainPage));
    }

    private static void ShowRegisterPage(string idToken, SocialService provider, string userJson) => MainWindow.Frame.Navigate(typeof(RegisterPage), new RegisterPageParameters(idToken, provider, ExtractNameFromUserJson(userJson)));
}
