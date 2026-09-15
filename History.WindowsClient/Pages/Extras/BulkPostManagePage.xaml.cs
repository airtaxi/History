using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class BulkPostManagePage : BasePage
{
    protected override BulkPostManagePageViewModel ViewModel { get; }

    public BulkPostManagePage()
    {
        ViewModel = App.Services.GetRequiredService<BulkPostManagePageViewModel>();

        InitializeComponent();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    // Infinite scroll: fetch the next page once the last cell gets realized.
    // Works even when the whole grid fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
