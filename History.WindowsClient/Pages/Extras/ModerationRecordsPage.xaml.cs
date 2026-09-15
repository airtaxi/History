using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class ModerationRecordsPage : BasePage, IRecipient<RefreshRequestedMessage>
{
    protected override ModerationRecordsPageViewModel ViewModel { get; }

    public ModerationRecordsPage()
    {
        ViewModel = App.Services.GetRequiredService<ModerationRecordsPageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register((IRecipient<RefreshRequestedMessage>)this);
    }

    // Refreshes the list for refresh requests from this window; requests from other
    // windows are routed away by the XamlRoot comparison.
    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground) _ = ViewModel.RefreshAsync();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    // Infinite scroll: fetch the next page once the last record's element gets realized.
    // Works even when the whole list fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
