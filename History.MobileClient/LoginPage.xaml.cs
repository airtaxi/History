using History.MobileClient.Login;

namespace History.MobileClient;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

#if ANDROID || IOS
    private async void OnLoginButtonClicked(object sender, EventArgs e)
#else
	private void OnLoginButtonClicked(object sender, EventArgs e)
#endif
    {
#if ANDROID || IOS
        var service = new GoogleAuthService();
		var token = await service.AuthenticateAsync();
		TokenLabel.Text = $"Token: {token}";
#endif
    }
}