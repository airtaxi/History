using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Numerics;

namespace History.WindowsClient.Pages;

public sealed partial class TimelinePage : BasePage, IRecipient<RefreshButtonClickedMessage>
{
    protected override TimelinePageViewModel ViewModel { get; }

    public TimelinePage()
    {
        ViewModel = App.Services.GetRequiredService<TimelinePageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    private bool _isInForeground;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
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

        await ViewModel.RefreshAsync();
    }

    private void OnMainScrollViewViewChanged(ScrollView sender, object args) => UpdateScrollToTopButtonVisibility(sender.VerticalOffset);

    // The InteractionTracker applies each mouse wheel notch directly with no inertia, so the
    // default wheel scrolling is far slower than ScrollViewer's. MouseWheel is excluded through
    // IgnoredInputKinds and converted here into an inertial velocity change instead. Touchpad
    // input is unaffected because it still goes through the CapableTouchpadOnly redirection.
    private const float MouseWheelVelocityPerDelta = 5.0f;
    private const float MouseWheelInertiaDecayRate = 0.95f;

    private void OnMainScrollViewPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var wheelDelta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
        if (wheelDelta == 0) return;

        MainScrollView.AddScrollVelocity(new Vector2(0, -wheelDelta * MouseWheelVelocityPerDelta), new Vector2(MouseWheelInertiaDecayRate, MouseWheelInertiaDecayRate));
    }

    // The floating button appears as soon as the feed moves away from the top and
    // hides again exactly at the top, so it never lingers over the first screen.
    private void UpdateScrollToTopButtonVisibility(double verticalOffset) => ScrollToTopButton.Visibility = verticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;

    private void OnScrollToTopButtonClicked(object sender, RoutedEventArgs e)
    {
        // Hide immediately so the button does not linger during the scroll itself.
        ScrollToTopButton.Visibility = Visibility.Collapsed;
        ScrollToVerticalOffsetAndRealize(0);
    }

    // ScrollingScrollOptions with AnimationMode.Disabled fires ScrollStarting synchronously on the
    // UI thread, which pre-realizes the target range through the ScrollPresenter's anticipated
    // viewport path. That pass fills the destination window before the compositor commits the new
    // offset, so no post-jump realization workaround is needed.
    private void ScrollToVerticalOffsetAndRealize(double verticalOffset)
    {
        var options = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
        MainScrollView.ScrollTo(0, verticalOffset, options);
    }
}