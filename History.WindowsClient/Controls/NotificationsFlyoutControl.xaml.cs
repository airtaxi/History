using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.Controls;

public sealed partial class NotificationsFlyoutControl : BaseControl
{
    public override NotificationsFlyoutViewModel ViewModel { get; }

    public NotificationsFlyoutControl()
    {
        ViewModel = App.Services.GetRequiredService<NotificationsFlyoutViewModel>();

        InitializeComponent();
    }

    protected override void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        base.OnControlLoaded(sender, e);

        // Subscribed after the base wiring so the flyout closes after the window navigates.
        ViewModel.NavigationRequested += OnViewModelNavigationRequested;
    }

    protected override void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigationRequested -= OnViewModelNavigationRequested;

        base.OnControlUnloaded(sender, e);
    }

    // Closes the hosting flyout so the navigated destination is visible behind it.
    private void OnViewModelNavigationRequested(object sender, NavigationRequestedEventArgs args)
    {
        DependencyObject node = this;
        while (node is not null and not Popup) node = VisualTreeHelper.GetParent(node);
        if (node is Popup popup) popup.IsOpen = false;
    }

    // Infinite scroll: fetch the next page once the last notification's element gets realized.
    private async void OnMainItemsRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Index != ViewModel.Items.Count - 1) return;

        await ViewModel.LoadMoreAsync();
    }
}
