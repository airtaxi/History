using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class MainPage : BasePage
{
    protected override MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();

        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.RefreshAsync();
    }
}
