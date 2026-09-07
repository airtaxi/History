using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.WindowsClient.Messages;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace History.WindowsClient.ViewModels.Notifications;

// Flyout-bound notification list view model: first-page refresh, infinite scroll pagination
// and the mark-all-as-read command. Loads quietly without the window loading overlay.
public partial class NotificationsFlyoutViewModel : BaseViewModel
{
    private const int PageSize = 30;

    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreNotificationsToLoad;

    [ObservableProperty]
    public partial ObservableCollection<NotificationViewModel> Items { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; private set; }

    // Unread count among the loaded items; drives the notification button badge.
    [ObservableProperty]
    public partial int UnreadCount { get; private set; }

    public bool IsEmpty => Items.Count == 0 && !IsLoading;

    // Tracks each item's read state so the unread count stays current while the flyout is open.
    public NotificationsFlyoutViewModel() => Items.CollectionChanged += OnItemsCollectionChanged;

    // Keeps the per-item subscriptions and the unread count in sync with the item list changes.
    private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (NotificationViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnNotificationItemPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (NotificationViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnNotificationItemPropertyChanged;
            }
        }

        RefreshUnreadCount();
    }

    // Recomputes the count immediately when an item is marked as read by any path.
    private void OnNotificationItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotificationViewModel.IsUnread))
        {
            RefreshUnreadCount();
        }
    }

    private void RefreshUnreadCount() => UnreadCount = Items.Count(x => x.IsUnread);

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            IsLoading = true;

            Items.Clear();
            _areThereNoMoreNotificationsToLoad = false;

            var notifications = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetNotifications(null, PageSize));
            foreach (var notification in notifications) Items.Add(new NotificationViewModel(notification, this));
        }
        catch (HttpRequestException) { }
        finally
        {
            IsLoading = false;
            _fetchSemaphore.Release();
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreNotificationsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            IsLoading = true;

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var notifications = await CommonShared.ApiHandler.ExecuteRequestAsync(new GetNotifications(lastViewModel.Notification.Id, PageSize));
            foreach (var notification in notifications) Items.Add(new NotificationViewModel(notification, this));

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
        var result = await ExecuteRequestAsync(new ReadAllNotifications());
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new NotificationsReadAllMessage());
    }
}
