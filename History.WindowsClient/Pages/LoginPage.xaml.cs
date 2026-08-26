using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace History.WindowsClient.Pages;

public sealed partial class LoginPage : BasePage
{
    protected override LoginPageViewModel ViewModel { get; }

    public LoginPage()
    {
        ViewModel = App.Services.GetRequiredService<LoginPageViewModel>();

        InitializeComponent();
    }
}
