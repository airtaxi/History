using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class BatchDeleteFriendsPage : BasePage
{
    protected override BatchDeleteFriendsPageViewModel ViewModel { get; }

    public BatchDeleteFriendsPage()
    {
        ViewModel = App.Services.GetRequiredService<BatchDeleteFriendsPageViewModel>();

        InitializeComponent();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    // Stops a running delete loop and keeps its result dialog and refresh from targeting a
    // page that is no longer on screen.
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedAway();

        base.OnNavigatedFrom(e);
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
