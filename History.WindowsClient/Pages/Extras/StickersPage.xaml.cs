using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class StickersPage : BasePage, IRecipient<RefreshRequestedMessage>, IRecipient<StickerCreateRequestedMessage>
{
    protected override StickersPageViewModel ViewModel { get; }

    public StickersPage()
    {
        ViewModel = App.Services.GetRequiredService<StickersPageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register((IRecipient<RefreshRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<StickerCreateRequestedMessage>)this);
    }

    // Refreshes the list for refresh requests from this window; requests from other
    // windows are routed away by the XamlRoot comparison.
    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground) _ = ViewModel.RefreshAsync();
    }

    // Opens the sticker create window for the window's add button, with the same window
    // routing as the refresh requests.
    public void Receive(StickerCreateRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground) ViewModel.OpenStickerCreate();
    }

    protected override async void OnFirstPageLoad() => await ViewModel.RefreshAsync();

    private async void OnStickerSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => await ViewModel.SearchAsync(args.QueryText);

    // Clearing the search box restores the full list without waiting for a submit.
    private async void OnStickerSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        if (!string.IsNullOrEmpty(sender.Text)) return;

        await ViewModel.SearchAsync(null);
    }

    // Infinite scroll: fetch the next page once the last sticker's element gets realized.
    // Works even when the whole list fits the viewport and no scrollbar exists.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
