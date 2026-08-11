using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace History.MobileClient.ViewModels;

// Base message view model shared by History and Kakao Story message types.
// Holds the full UI surface used by the shared templates and virtual command entry points.
// Derived types fill the surface and override behavior; commands are declared here only
// (adding [RelayCommand] on overrides would create duplicate command names).
public partial class BaseMessageViewModel : ObservableObject
{
    public virtual string Id => null;
    public virtual string SenderName => null;
    public virtual bool IsSenderAdmin => false;
    public virtual bool IsSenderModerator => false;
    public virtual IMediaViewModel SenderProfileMedia => null;
    public virtual IMediaViewModel ReceiverProfileMedia => null;
    public virtual string ReceiverName => null;
    public virtual bool IsReceiverAdmin => false;
    public virtual bool IsReceiverModerator => false;
    public virtual bool IsUnread => false;
    public virtual string MainText => null;
    public virtual string ImageUrl => null;
    public virtual bool HasImage => false;
    public virtual string TimestampText => null;
    public virtual bool IsReplyButtonVisible => true;
    // Delete is only supported by the Kakao Story message type; History keeps the button hidden.
    public virtual bool IsDeleteButtonVisible => false;

    [RelayCommand]
    public virtual async Task OpenMessageAsync() => throw new NotSupportedException("[BaseMessageViewModel] OpenMessageAsync must be overridden");

    [RelayCommand]
    public virtual async Task HandleProfileTapAsync() => throw new NotSupportedException("[BaseMessageViewModel] HandleProfileTapAsync must be overridden");

    [RelayCommand]
    public virtual async Task DeleteAsync(bool popModal) => throw new NotSupportedException("[BaseMessageViewModel] DeleteAsync must be overridden");
}
