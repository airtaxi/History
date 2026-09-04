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
    // View models are cached per user id instead of caching the page itself: a cached
    // page would keep showing the previous user's profile when navigated to with a
    // different user id, while a fresh page bound to the cached view model preserves
    // the feed and scroll position without any stale content.
    private static readonly Dictionary<string, ProfilePageViewModel> ViewModelCache = [];

    private ProfilePageViewModel _viewModel;
    private bool _shouldRestoreScroll;

    protected override ProfilePageViewModel ViewModel => _viewModel!;

    public ProfilePage()
    {
        _viewModel = App.Services.GetRequiredService<ProfilePageViewModel>();

        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    private bool _isInForeground;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is string userId)
        {
            if (!ViewModelCache.TryGetValue(userId, out var cachedViewModel))
            {
                cachedViewModel = _viewModel;
                cachedViewModel.Initialize(userId);
                ViewModelCache[userId] = cachedViewModel;
            }
            else
            {
                _shouldRestoreScroll = cachedViewModel.ScrollHeight > 0;
                _viewModel = cachedViewModel;
            }
        }

        base.OnNavigatedTo(e);

        _isInForeground = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _isInForeground = false;

        // Leaving through back navigation removes this page from the frame history,
        // so its cached view model can never be revisited and is released.
        if (e.NavigationMode == NavigationMode.Back && ViewModel.UserId is string userId) ViewModelCache.Remove(userId);
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
            MainScrollViewer.ScrollToVerticalOffset(ViewModel.ScrollHeight);
            _shouldRestoreScroll = false;
            return;
        }

        // Fire-and-forget like the friend-notification read clearing; the feed refresh
        // does not wait for it.
        _ = ViewModel.MarkFriendNotificationsAsReadAsync();
        await ViewModel.RefreshAsync();
    }

    // Captures the vertical offset continuously so leaving and revisiting the same
    // profile can restore the reading position.
    private void OnMainScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Layout passes running before the stored offset is restored would overwrite
        // it with the initial zero, so capture only after the restore point.
        if (!_isInForeground || _shouldRestoreScroll) return;

        ViewModel.ScrollHeight = ((ScrollViewer)sender).VerticalOffset;
    }

    private async void OnRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args) => await ViewModel.RefreshAsync();
}
