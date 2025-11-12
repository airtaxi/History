using History.Commons;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.MobileClient.Helpers;
using System.Threading.Tasks;

namespace History.MobileClient.Pages
{
    public partial class RegisterPage : ContentPage
    {
        private bool _termsCompleted;
        private bool _privacyAgreementCompleted;
        private bool _ageCompleted;

        private readonly string _idToken;
        private readonly string _name;
        private readonly SocialService _socialService;

        public RegisterPage(string idToken, SocialService socialService, string name = null)
        {
            InitializeComponent();
            _idToken = idToken;
            _socialService = socialService;
            _name = name;
        }

        private void ValidateStatus() => RegisterButton.IsEnabled = _termsCompleted && _privacyAgreementCompleted && _ageCompleted;

        private async void OnViewTermsLabelTapped(object sender, TappedEventArgs e)
        {
#if IOS
            await Browser.Default.OpenAsync("https://history.cenox.io/terms.html", BrowserLaunchMode.SystemPreferred);
#else
            var page = new InAppBrowserPage("서비스 이용 약관", "https://history.cenox.io/terms.html");
            await App.PushModalAsync(page);
#endif
        }

        private async void OnViewPrivacyAgreementTermsLabelTapped(object sender, TappedEventArgs e)
        {
#if IOS
            await Browser.Default.OpenAsync("https://history.cenox.io/privacyagreement.html", BrowserLaunchMode.SystemPreferred);
#else
            var page = new InAppBrowserPage("개인정보 수집·이용 동의", "https://history.cenox.io/privacyagreement.html");
            await App.PushModalAsync(page);
#endif
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
            var result = await App.ExecuteRequestAsync(new Register(_idToken, _socialService, _name));
            if (result.IsSuccess)
            {
                await App.Page.DisplayAlertAsync("안내", "가입이 완료되었습니다.", Constants.PromptOk);
                await LoginPage.Login(_idToken, _socialService);
            }
        }

        private void OnTermsLabelTapped(object sender, TappedEventArgs e) => TermsCheckBox.IsChecked = !TermsCheckBox.IsChecked;
        private void OnPrivacyAgreementLabelTapped(object sender, TappedEventArgs e) => PrivacyAgreementCheckBox.IsChecked = !PrivacyAgreementCheckBox.IsChecked;
        private void OnAgeLabelTapped(object sender, TappedEventArgs e) => AgeCheckBox.IsChecked = !AgeCheckBox.IsChecked;

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
            if (safeAreaTopHeight != 0)
            {
                var statusBarHeight = LayoutHelper.GetStatusBarHeight();
                Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
            }
        }

        private void OnLoaded(object sender, EventArgs e)
        {
#if IOS
            AppleSwipeGestureHelper.ApplyToPage(this);
#endif
        }
    }
}
