using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class InviteCodeRequestsPage : BasePage, IRecipient<RefreshRequestedMessage>
{
    protected override InviteCodeRequestsPageViewModel ViewModel { get; }

    public InviteCodeRequestsPage()
    {
        ViewModel = App.Services.GetRequiredService<InviteCodeRequestsPageViewModel>();

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

    // Clearing the incoming request notifications needs no dialog surface, so it runs as
    // soon as the page is navigated to; the list load waits for the first load so the
    // loading overlay and dialogs have a live XamlRoot.
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _ = ViewModel.MarkNotificationsAsReadAsync();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    // Infinite scroll: fetch the next page once the last request's element gets realized.
    // Works even when the whole list fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
