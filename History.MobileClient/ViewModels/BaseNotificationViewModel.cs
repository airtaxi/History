using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.MobileClient.DataTypes;

namespace History.MobileClient.ViewModels;

// Base notification view model shared by History and Kakao Story notification types.
// Holds the UI surface used by the shared notification template and virtual command entry points.
// Derived types implement the surface and override behavior; commands are declared here only
// (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseNotificationViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAcceptButtonVisible))]
    public partial bool IsAccepted { get; protected set; }

    // Default surface values — overridden by derived types.
    public virtual bool IsUnread => false;
    public virtual string Title => null;
    public virtual string Body => null;
    public virtual bool IsBodyVisible => false;
    public virtual string TimestampText => null;
    public virtual ImageViewModel ImageMedia => null;
    public virtual bool IsImageVisible => false;
    public virtual bool IsFriendRequest => false;
    public virtual bool IsAcceptButtonVisible => IsFriendRequest && !IsAccepted;
    public virtual IMediaViewModel ProfileMedia => null;

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseNotificationViewModel] HandleTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileTapAsync() => throw new NotSupportedException("[BaseNotificationViewModel] HandleProfileTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task AcceptFriendRequestAsync() => throw new NotSupportedException("[BaseNotificationViewModel] AcceptFriendRequestAsync must be overridden");

    [RelayCommand]
    public virtual async Task MarkAsReadAsync() => throw new NotSupportedException("[BaseNotificationViewModel] MarkAsReadAsync must be overridden");
}
