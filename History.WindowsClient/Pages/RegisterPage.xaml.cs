using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class RegisterPage : BasePage
{
    protected override RegisterPageViewModel ViewModel { get; }

    public RegisterPage()
    {
        ViewModel = App.Services.GetRequiredService<RegisterPageViewModel>();

        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is RegisterPageParameters parameters) ViewModel.Initialize(parameters);
    }
}
