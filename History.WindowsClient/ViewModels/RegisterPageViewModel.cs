using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Services;
using History.WindowsClient.Views;

namespace History.WindowsClient.ViewModels;

public partial class RegisterPageViewModel(ApplicationSettingsService settingsService) : BaseViewModel
{
    private const string TermsUrl = "https://history.cenox.io/terms.html";
    private const string PrivacyAgreementUrl = "https://history.cenox.io/privacyagreement.html";
    private const string DefaultDialogPrimaryButtonText = "확인";

    private string _idToken;
    private SocialService _socialService;
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegisterButtonEnabled))]
    public partial bool TermsCompleted { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegisterButtonEnabled))]
    public partial bool PrivacyAgreementCompleted { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegisterButtonEnabled))]
    public partial bool AgeCompleted { get; set; }

    [ObservableProperty]
    public partial string InviteCode { get; set; }

    public bool IsRegisterButtonEnabled => TermsCompleted && PrivacyAgreementCompleted && AgeCompleted;

    public void Initialize(RegisterPageParameters parameters)
    {
        _idToken = parameters.IdToken;
        _socialService = parameters.SocialService;
        _name = parameters.Name;
    }

    partial void OnInviteCodeChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || value == value.ToUpper()) return;
        InviteCode = value.ToUpper();
    }

    [RelayCommand]
    private void ViewTerms() => MainWindow.Frame.Navigate(typeof(BrowserPage), new BrowserPageParameters(TermsUrl));

    [RelayCommand]
    private void ViewPrivacyAgreement() => MainWindow.Frame.Navigate(typeof(BrowserPage), new BrowserPageParameters(PrivacyAgreementUrl));

    [RelayCommand]
    private async Task RegisterAsync()
    {
        var inviteCode = InviteCode?.Trim();
        var result = await App.ExecuteRequestAsync(new Register(_idToken, _socialService, _name, inviteCode), [ErrorType.BadRequest, ErrorType.NotFound, ErrorType.Conflict]);

        if (result.IsSuccess)
        {
            await ShowMessageDialogAsync(new("안내", "가입이 완료되었습니다."));

            CommonShared.ApiHandler = new(result.Value.AccessToken, result.Value.RefreshToken);
            settingsService.Settings.AccessToken = result.Value.AccessToken;
            settingsService.Settings.RefreshToken = result.Value.RefreshToken;

            await LoginPageViewModel.LoadMyProfileAsync();
            LoginPageViewModel.NavigateToMainPage();
        }
        else if (result.Error == ErrorType.BadRequest || result.Error == ErrorType.NotFound || result.Error == ErrorType.Conflict) await ShowMessageDialogAsync(new("오류", result.ErrorMessage));
    }
}
