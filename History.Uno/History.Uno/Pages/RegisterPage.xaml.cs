using History.Commons.Api.User;
using History.Commons.Enums;
using History.Uno.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace History.Uno.Pages;

/// <summary>
/// Registration page — requires invite code and agreement to terms.
/// Receives (idToken, socialService, name) via navigation parameter.
/// </summary>
public sealed partial class RegisterPage : Page
{
    private bool _termsCompleted;
    private bool _privacyAgreementCompleted;
    private bool _ageCompleted;

    private string _idToken;
    private string _name;
    private SocialService _socialService;

    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is (string idToken, SocialService socialService, string name))
        {
            _idToken = idToken;
            _socialService = socialService;
            _name = name;
        }
    }

    private void ValidateStatus() => RegisterButton.IsEnabled = _termsCompleted && _privacyAgreementCompleted && _ageCompleted;

    private void OnCheckBoxChecked(object sender, RoutedEventArgs e)
    {
        if (sender == TermsCheckBox) _termsCompleted = true;
        else if (sender == PrivacyAgreementCheckBox) _privacyAgreementCompleted = true;
        else if (sender == AgeCheckBox) _ageCompleted = true;
        ValidateStatus();
    }

    private void OnCheckBoxUnchecked(object sender, RoutedEventArgs e)
    {
        if (sender == TermsCheckBox) _termsCompleted = false;
        else if (sender == PrivacyAgreementCheckBox) _privacyAgreementCompleted = false;
        else if (sender == AgeCheckBox) _ageCompleted = false;
        ValidateStatus();
    }

    private void OnTermsTextBlockTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        TermsCheckBox.IsChecked = !TermsCheckBox.IsChecked;
    }

    private void OnPrivacyAgreementTextBlockTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        PrivacyAgreementCheckBox.IsChecked = !PrivacyAgreementCheckBox.IsChecked;
    }

    private void OnAgeTextBlockTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        AgeCheckBox.IsChecked = !AgeCheckBox.IsChecked;
    }

    private async void OnViewTermsTextBlockTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
#if __IOS__
        await Launcher.LaunchUriAsync(new Uri("https://history.cenox.io/terms.html"));
#else
        await App.PushAsync(typeof(InAppBrowserPage), ("서비스 이용 약관", "https://history.cenox.io/terms.html"));
#endif
    }

    private async void OnViewPrivacyAgreementTermsTextBlockTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
#if __IOS__
        await Launcher.LaunchUriAsync(new Uri("https://history.cenox.io/privacyagreement.html"));
#else
        await App.PushAsync(typeof(InAppBrowserPage), ("개인정보 수집·이용 동의", "https://history.cenox.io/privacyagreement.html"));
#endif
    }

    private async void OnRegisterButtonClicked(object sender, RoutedEventArgs e)
    {
        var inviteCode = InviteCodeTextBox.Text?.Trim();
        var result = await App.ExecuteRequestAsync(new Register(_idToken, _socialService, _name, inviteCode), [ErrorType.BadRequest, ErrorType.NotFound, ErrorType.Conflict]);

        if (result.IsSuccess)
        {
            await App.DisplayAlertAsync("안내", "가입이 완료되었습니다.", Constants.PromptOk);
            await LoginService.LoginAsync(_idToken, _socialService);
        }
        else if (result.Error is ErrorType.BadRequest or ErrorType.NotFound or ErrorType.Conflict) await App.DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
    }

    private void OnInviteCodeTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text) && textBox.Text != textBox.Text.ToUpper())
        {
            textBox.Text = textBox.Text.ToUpper();
            textBox.SelectionStart = textBox.Text.Length;
        }
    }
}