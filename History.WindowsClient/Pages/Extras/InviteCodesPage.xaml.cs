using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class InviteCodesPage : BasePage, IRecipient<RefreshRequestedMessage>, IRecipient<InviteCodeRequestRequestedMessage>
{
    protected override InviteCodesPageViewModel ViewModel { get; }

    public InviteCodesPage()
    {
        ViewModel = App.Services.GetRequiredService<InviteCodesPageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register((IRecipient<RefreshRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<InviteCodeRequestRequestedMessage>)this);
    }

    // Refreshes the list for refresh requests from this window; requests from other
    // windows are routed away by the XamlRoot comparison.
    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground) _ = ViewModel.RefreshAsync();
    }

    // Runs the invite code request flow for the window's add button, with the same window
    // routing as the refresh requests.
    public void Receive(InviteCodeRequestRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground) _ = ViewModel.RequestInviteCodesAsync();
    }

    // Clearing the request-result notifications needs no dialog surface, so it runs as soon
    // as the page is navigated to; the list load waits for the first load so the loading
    // overlay and dialogs have a live XamlRoot.
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _ = ViewModel.MarkNotificationsAsReadAsync();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    // Infinite scroll: fetch the next page once the last code's element gets realized.
    // Works even when the whole list fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
