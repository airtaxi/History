using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.WindowsClient.Messages;
using System.Collections.ObjectModel;

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

    public bool IsEmpty => Items.Count == 0 && !IsLoading;

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
