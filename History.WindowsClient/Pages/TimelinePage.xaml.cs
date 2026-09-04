using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class TimelinePage : BasePage
{
    protected override TimelinePageViewModel ViewModel { get; }

    public TimelinePage()
    {
        ViewModel = App.Services.GetRequiredService<TimelinePageViewModel>();

        InitializeComponent();
    }

    // Infinite scroll: fetch the next page once the last post's element gets realized.
    // Works even when the whole feed fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private bool _isFirstLoad;
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        await ViewModel.RefreshAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}