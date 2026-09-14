using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace History.WindowsClient.ViewModels.Notifications;

// Notification list view model: first-page refresh, infinite scroll pagination and the
// mark-all-as-read command. Loads quietly without the window loading overlay. The source
// switches with the account mode: Kakao Story mode shows Kakao Story notifications.
// The per-platform unread counts live here as the single source of truth: the title bar
// badge shows the active platform's count and the main page mode selector badges show
// each platform's count.
public partial class NotificationsViewModel : BaseViewModel, IRecipient<KakaoStoryModeChangedMessage>
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreNotificationsToLoad;

    [ObservableProperty]
    public partial ObservableCollection<BaseNotificationViewModel> Items { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; private set; }

    // Unread count among the loaded items for the active platform; drives the notification button badge.
    [ObservableProperty]
    public partial int UnreadCount { get; private set; }

    // Per-platform unread notification counts; the badge poller refreshes both every cycle so
    // the inactive platform's mode selector badge stays current as well.
    [ObservableProperty]
    public partial int HistoryUnreadCount { get; private set; }

    [ObservableProperty]
    public partial int KakaoStoryUnreadCount { get; private set; }

    public bool IsEmpty => Items.Count == 0 && !IsLoading;

    // Tracks each item's read state so the unread count stays current while the flyout is open.
    public NotificationsViewModel()
    {
        Items.CollectionChanged += OnItemsCollectionChanged;
        WeakReferenceMessenger.Default.Register(this);
    }

    // Refreshes the list and the badge when the account mode switches so the flyout always
    // shows the active platform's notifications.
    public void Receive(KakaoStoryModeChangedMessage message)
    {
        UpdateUnreadCount();
        _ = RefreshAsync();
    }

    // Keeps the per-item subscriptions and the unread count in sync with the item list changes.
    private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (BaseNotificationViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnNotificationItemPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (BaseNotificationViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnNotificationItemPropertyChanged;
            }
        }

        RefreshUnreadCount();
    }

    // Recomputes the count immediately when an item is marked as read by any path.
    private void OnNotificationItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BaseNotificationViewModel.IsUnread))
        {
            RefreshUnreadCount();
        }
    }

    // The loaded items belong to the active platform, so their unread count updates only it.
    private void RefreshUnreadCount()
    {
        if (CommonShared.LastUsedKakaoStoryMode) KakaoStoryUnreadCount = Items.Count(x => x.IsUnread);
        else HistoryUnreadCount = Items.Count(x => x.IsUnread);
    }

    // The title bar badge always shows the active platform's count.
    private void UpdateUnreadCount() => UnreadCount = CommonShared.LastUsedKakaoStoryMode ? KakaoStoryUnreadCount : HistoryUnreadCount;

    partial void OnHistoryUnreadCountChanged(int value) => UpdateUnreadCount();
    partial void OnKakaoStoryUnreadCountChanged(int value) => UpdateUnreadCount();

    // Refreshes the first page of the active platform's notifications. When the Kakao Story
    // session expired, the login prompt appears only when the user opens the flyout; the
    // badge refresh stays silent.
    public async Task RefreshAsync(bool promptLoginOnExpiredSession = false)
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            IsLoading = true;

            Items.Clear();
            _areThereNoMoreNotificationsToLoad = false;

            if (!CommonShared.LastUsedKakaoStoryMode)
            {
                var notifications = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetNotifications(null, Constants.PageSize));
                foreach (var notification in notifications) Items.Add(new HistoryNotificationViewModel(notification, this));
            }
            else
            {
                var notifications = await FetchKakaoStoryNotificationsAsync(promptLoginOnExpiredSession);
                if (notifications == null) return;

                foreach (var notification in notifications) Items.Add(new KakaoStoryNotificationViewModel(notification, this));
            }
        }
        catch (HttpRequestException) { }
        finally
        {
            IsLoading = false;
            _fetchSemaphore.Release();
        }
    }

    // Quiet badge refresh for the title bar badge poller: fetches both platforms' notification
    // lists in parallel and updates only the unread counts, leaving the loaded list untouched.
    // The list refresh semaphore is shared so a poll cycle never overlaps a list refresh, and
    // a failed cycle is skipped silently.
    public async Task RefreshUnreadCountAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var historyTask = FetchHistoryUnreadCountAsync();
            var kakaoStoryTask = FetchKakaoStoryUnreadCountAsync();
            await Task.WhenAll(historyTask, kakaoStoryTask);
        }
        catch (HttpRequestException) { }
        finally { _fetchSemaphore.Release(); }
    }

    // Fetches the History notification list quietly; skipped while signed out.
    private async Task FetchHistoryUnreadCountAsync()
    {
        if (CommonShared.ApiHandler == ApiHandler.Public) return;

        var notifications = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetNotifications(null, Constants.PageSize));

        // Signed out while the request was in flight: the sign-out badge reset must stand.
        if (CommonShared.ApiHandler == ApiHandler.Public) return;

        HistoryUnreadCount = notifications?.Count(x => x.IsUnread) ?? 0;
    }

    // Fetches the Kakao Story notification list quietly; a missing session leaves the count unchanged.
    private async Task FetchKakaoStoryUnreadCountAsync()
    {
        var notifications = await FetchKakaoStoryNotificationsAsync(promptLoginOnExpiredSession: false);
        if (notifications == null) return;

        // Signed out while the request was in flight: the sign-out badge reset must stand.
        if (CommonShared.ApiHandler == ApiHandler.Public) return;

        KakaoStoryUnreadCount = notifications.Count(x => x.is_new);
    }

    // Clears the badge counts on sign-out so the badges hide immediately and the next login
    // refreshes them from the new session.
    public void ResetUnreadCount()
    {
        HistoryUnreadCount = 0;
        KakaoStoryUnreadCount = 0;
    }

    // Fetches the full Kakao Story notification list. A missing or expired token skips the
    // fetch silently unless the caller wants the login flow. Background mode keeps a revoked
    // session from popping the login modal during the quiet badge refresh.
    private async Task<List<KakaoStoryApiHandler.DataType.Notification>> FetchKakaoStoryNotificationsAsync(bool promptLoginOnExpiredSession)
    {
        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() == null)
        {
            if (!promptLoginOnExpiredSession) return null;
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return null;
        }

        var previousBackgroundMode = KakaoStoryApiHandler.IsBackgroundMode;
        if (!promptLoginOnExpiredSession) KakaoStoryApiHandler.IsBackgroundMode = true;
        try { return await KakaoStoryApiHandler.GetNotifications(); }
        finally { KakaoStoryApiHandler.IsBackgroundMode = previousBackgroundMode; }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        // Kakao Story returns the full list in one call, so there is no pagination.
        else if (CommonShared.LastUsedKakaoStoryMode) return;
        else if (_areThereNoMoreNotificationsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            IsLoading = true;

            var lastViewModel = Items.OfType<HistoryNotificationViewModel>().LastOrDefault();
            if (lastViewModel == null) return;

            var notifications = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetNotifications(lastViewModel.Notification.Id, Constants.PageSize));
            foreach (var notification in notifications) Items.Add(new HistoryNotificationViewModel(notification, this));

            if (notifications.Count == 0) _areThereNoMoreNotificationsToLoad = true;
        }
        catch (HttpRequestException) { }
        finally
        {
            IsLoading = false;
            _fetchSemaphore.Release();
        }
    }

    [RelayCommand]
    public async Task ReadAllAsync()
    {
        if (CommonShared.LastUsedKakaoStoryMode)
        {
            await ReadAllKakaoStoryNotificationsAsync();
            return;
        }

        var result = await ExecuteRequestAsync(new ReadAllNotifications());
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new NotificationsReadAllMessage());
    }

    // Kakao Story has no read-all endpoint; fetching each notification target marks it as
    // read server-side, so every unread item is marked by fetching its target in parallel.
    private async Task ReadAllKakaoStoryNotificationsAsync()
    {
        var unreadViewModels = Items.Where(x => x.IsUnread).ToList();
        if (unreadViewModels.Count == 0) return;

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                await Task.WhenAll(unreadViewModels.Select(x => x.MarkAsReadAsync()));
                return true;
            });
        }
        catch (Exception exception)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"알림 읽음 처리에 실패하였습니다.\n{exception.Message}"));
            return;
        }

        await ShowMessageDialogAsync(new MessageDialogParameters("알림", "모든 알림을 읽음 처리했습니다."));
    }
}
