using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Pages;

public sealed partial class LoginPage : BasePage
{
    protected override LoginPageViewModel ViewModel { get; }

    public LoginPage()
    {
        ViewModel = App.Services.GetRequiredService<LoginPageViewModel>();

        InitializeComponent();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.TryAutoLoginAsync();
}