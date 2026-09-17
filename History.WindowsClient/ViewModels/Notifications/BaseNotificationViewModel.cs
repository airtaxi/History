using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels.Notifications;

// Base notification list item view model: the display surface shared by the History and
// Kakao Story notification items in the notifications flyout template, plus the tap entry
// point. Derived types own their DTO and implement the surface and read-state behavior.
public abstract partial class BaseNotificationViewModel : BaseViewModel
{
    protected BaseNotificationViewModel(bool isUnread) => IsUnread = isUnread;

    [ObservableProperty]
    public partial bool IsUnread { get; private set; }

    // A friend request row hides its accept button once the request has been answered; the
    // notification data can already carry an accepted status when it was answered elsewhere.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAcceptButtonVisible))]
    public partial bool IsAccepted { get; protected set; }

    public abstract string Title { get; }
    public abstract string Body { get; }
    public abstract bool IsBodyVisible { get; }
    public abstract string TimestampText { get; }
    public abstract bool IsImageVisible { get; }
    public abstract ImageSource ProfileImageSource { get; }
    public abstract ImageSource ImageSource { get; }

    // Friend request rows swap their thumbnail for an accept button until the request is answered.
    public virtual bool IsFriendRequest => false;
    public virtual bool IsAcceptButtonVisible => IsFriendRequest && !IsAccepted;

    // Entry point for notification taps: each platform navigates to its target and marks
    // the notification as read.
    [RelayCommand]
    public virtual Task HandleTapAsync() => Task.CompletedTask;

    // Entry point for friend request rows: accepts the request without navigating anywhere.
    [RelayCommand]
    public virtual Task AcceptFriendRequestAsync() => Task.CompletedTask;

    public virtual Task MarkAsReadAsync() => Task.CompletedTask;

    // Keeps the DTO and the bindable surface in sync when the read state changes.
    protected void SetUnread(bool value)
    {
        OnUnreadStateChanged(value);
        IsUnread = value;
    }

    protected virtual void OnUnreadStateChanged(bool value) { }
}
