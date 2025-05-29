using History.Commons;
using History.Commons.Api.User;
using History.Commons.Enums;
using System.Threading.Tasks;

namespace History.MobileClient.Pages
{
    public partial class RegisterPage : ContentPage
    {
        private bool _codeCompleted;
        private bool _termsCompleted;
        private bool _privacyAgreementCompleted;
        private bool _ageCompleted;

        private readonly string _idToken;
        private readonly SocialService _socialService;

        public RegisterPage(string idToken, SocialService socialService)
        {
            InitializeComponent();
            _idToken = idToken;
            _socialService = socialService;
        }

        private void ValidateStatus() => RegisterButton.IsEnabled = _codeCompleted && _termsCompleted && _privacyAgreementCompleted && _ageCompleted;

        private async void OnViewTermsLabelTapped(object sender, TappedEventArgs e)
        {
            var page = new InAppBrowserPage("서비스 이용 약관", "https://history.cenox.io/terms.html");
            await App.PushModalAsync(page);
        }

        private async void OnViewPrivacyAgreementTermsLabelTapped(object sender, TappedEventArgs e)
        {
            var page = new InAppBrowserPage("개인정보 수집·이용 동의", "https://history.cenox.io/privacyagreement.html");
            await App.PushModalAsync(page);
        }

        private void OnCodeEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            _codeCompleted = !string.IsNullOrWhiteSpace(entry?.Text);

            ValidateStatus();
        }

        private void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if(checkBox == TermsCheckBox) _termsCompleted = checkBox.IsChecked;
            else if(checkBox == PrivacyAgreementCheckBox) _privacyAgreementCompleted = checkBox.IsChecked;
            else if(checkBox == AgeCheckBox) _ageCompleted = checkBox.IsChecked;

            ValidateStatus();
        }

        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            var result = await App.ExecuteRequestAsync(new Register(CodeEntry.Text, _idToken, _socialService));
            if (result.IsSuccess) await App.Page.DisplayAlert("안내", "가입이 완료되었습니다.", Constants.PromptOk);

            await LoginPage.Login(_idToken, _socialService);
        }
    }
}
