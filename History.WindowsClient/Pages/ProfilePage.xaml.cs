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

// Profile page: dispatches between the History profile and the Kakao Story profile.
// View models are cached per user id instead of caching the page itself: a cached
// page would keep showing the previous user's profile when navigated to with a
// different user id, while a fresh page bound to the cached view model preserves
// the feed and scroll position without any stale content.
public sealed partial class ProfilePage : BasePage, IRecipient<RefreshButtonClickedMessage>
{
    private static readonly Dictionary<string, HistoryProfilePageViewModel> HistoryViewModelCache = [];

    private HistoryProfilePageViewModel _historyViewModel;
    private bool _shouldRestoreScroll;

    protected override BaseProfilePageViewModel ViewModel => _activeViewModel;

    // The user id of the profile being shown, used by the window to skip
    // redundant navigation to the same user's profile.
    public string UserId => ViewModel.UserId;

    // Whether the page currently shows a Kakao Story profile. The History and Kakao
    // Story user id spaces are separate, so the window distinguishes them here.
    public bool IsKakaoStoryPage { get; private set; }

    public ProfilePage()
    {
        _historyViewModel = App.Services.GetRequiredService<HistoryProfilePageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    private BaseProfilePageViewModel _activeViewModel;
    private bool _isInForeground;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string userId)
        {
            if (!HistoryViewModelCache.TryGetValue(userId, out var cachedViewModel))
            {
                cachedViewModel = _historyViewModel;
                cachedViewModel.Initialize(userId);
                HistoryViewModelCache[userId] = cachedViewModel;
            }
            else
            {
                _shouldRestoreScroll = cachedViewModel.ScrollHeight > 0;
                _historyViewModel = cachedViewModel;
            }
            _activeViewModel = _historyViewModel;
        }
        else if (!TryInitializeKakaoStory(e.Parameter)) _activeViewModel = _historyViewModel;

        base.OnNavigatedTo(e);

        _isInForeground = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _isInForeground = false;

        // Leaving through back navigation removes this page from the frame history,
        // so its cached view model can never be revisited and is released.
        if (e.NavigationMode == NavigationMode.Back)
        {
            if (ViewModel is HistoryProfilePageViewModel) HistoryViewModelCache.Remove(ViewModel.UserId);
            else if (ViewModel is KakaoProfilePageViewModel) ReleaseKakaoViewModel();
        }
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

        // A cached view model already holds the loaded feed, so restore its stored
        // scroll offset instead of reloading, which would reset the position.
        if (ViewModel.Profile != null)
        {
            UpdateLayout();
            ScrollToVerticalOffsetAndRealize(ViewModel.ScrollHeight);
            UpdateScrollToTopButtonVisibility(ViewModel.ScrollHeight);
            _shouldRestoreScroll = false;
            return;
        }

        // Fire-and-forget like the friend-notification read clearing; the feed refresh
        // does not wait for it.
        if (ViewModel is HistoryProfilePageViewModel historyViewModel) _ = historyViewModel.MarkFriendNotificationsAsReadAsync();
        await ViewModel.RefreshAsync();
    }

    // Captures the vertical offset continuously so leaving and revisiting the same
    // profile can restore the reading position.
    private void OnMainScrollViewViewChanged(ScrollView sender, object args)
    {
        // Layout passes running before the stored offset is restored would overwrite
        // it with the initial zero, so capture only after the restore point.
        if (_isInForeground && !_shouldRestoreScroll) ViewModel.ScrollHeight = sender.VerticalOffset;

        UpdateScrollToTopButtonVisibility(sender.VerticalOffset);
    }

    // The InteractionTracker applies each mouse wheel notch directly with no inertia, so the
    // default wheel scrolling is far slower than ScrollViewer's. MouseWheel is excluded through
    // IgnoredInputKinds and converted here into an inertial velocity change instead. Most
    // touchpad input stays on the tracker, but wheel events still leak through at gesture
    // boundaries with arbitrary deltas, so those are filtered out in the handler below.
    private const float MouseWheelVelocityPerDelta = 5.0f;
    private const float MouseWheelInertiaDecayRate = 0.95f;

    private void OnMainScrollViewPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var wheelDelta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
        if (wheelDelta == 0) return;

        // Physical wheels always deliver multiples of 120, while touchpad deltas are
        // arbitrary values, so non-multiples are ignored as touchpad input.
        if (wheelDelta % 120 != 0) return;

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
