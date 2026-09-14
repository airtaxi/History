using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class SearchResultPage : BasePage, IRecipient<RefreshRequestedMessage>
{
    protected override SearchResultPageViewModel ViewModel { get; }

    public SearchResultPage()
    {
        ViewModel = App.Services.GetRequiredService<SearchResultPageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string query) ViewModel.Initialize(query);

        base.OnNavigatedTo(e);
    }

    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground)
        {
            _ = ViewModel.RefreshAsync();
        }
    }

    // Infinite scroll: fetch the next page once the last post's element gets realized.
    // Works even when the whole feed fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
