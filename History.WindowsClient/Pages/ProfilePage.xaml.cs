using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class ProfilePage : BasePage, IRecipient<RefreshButtonClickedMessage>
{
    protected override ProfilePageViewModel ViewModel { get; }

    public ProfilePage()
    {
        ViewModel = App.Services.GetRequiredService<ProfilePageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    private bool _isInForeground;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string userId) ViewModel.Initialize(userId);

        base.OnNavigatedTo(e);

        _isInForeground = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _isInForeground = false;
    }

    public void Receive(RefreshButtonClickedMessage message)
    {
        if (_isInForeground)
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

    private bool _isFirstLoad;
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        // Fire-and-forget like the friend-notification read clearing; the feed refresh
        // does not wait for it.
        _ = ViewModel.MarkFriendNotificationsAsReadAsync();
        await ViewModel.RefreshAsync();
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
