using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Pages;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.Notifications;

// Notification list item view model: holds the DTO and the display surface used by the
// notifications flyout template.
public partial class NotificationViewModel : BaseViewModel, IRecipient<NotificationsReadAllMessage>, IRecipient<NotificationPostReadMessage>, IRecipient<NotificationFriendUserReadMessage>
{
    private readonly BaseViewModel _baseViewModel;

    public NotificationResponseDto Notification { get; }

    [ObservableProperty]
    public partial bool IsUnread { get; private set; }

    public string Title => Notification.Title;
    public string Body => Notification.Body;
    public bool IsBodyVisible => !string.IsNullOrEmpty(Notification.Body);
    public string TimestampText => PostHelper.GenerateFriendlyTimestamp(Notification.CreatedAt, null);
    public bool IsImageVisible => !string.IsNullOrEmpty(Notification.ImageUrl);

    public ImageSource ProfileImageSource => Notification.User?.ProfileThumbnailMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(Notification.User.ProfileThumbnailMediaId)));
    public ImageSource ImageSource => string.IsNullOrEmpty(Notification.ImageUrl) ? null : new BitmapImage(new Uri(Notification.ImageUrl));

    public NotificationViewModel(NotificationResponseDto notification, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        Notification = notification;
        IsUnread = notification.IsUnread;

        WeakReferenceMessenger.Default.Register((IRecipient<NotificationsReadAllMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationPostReadMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationFriendUserReadMessage>)this);
    }

    public void Receive(NotificationsReadAllMessage message)
    {
        if (!IsUnread) return;
        SetUnread(false);
    }

    public void Receive(NotificationPostReadMessage message)
    {
        if (Notification.Data == null || !Notification.Data.TryGetValue("PostId", out var postId)) return;
        if (postId != message.Value) return;
        SetUnread(false);
    }

    public void Receive(NotificationFriendUserReadMessage message)
    {
        if (Notification.Data == null || !Notification.Data.TryGetValue("UserId", out var userId)) return;
        if (userId != message.Value) return;
        SetUnread(false);
    }

    // Entry point for notification taps: navigates to the notification target and marks the
    // notification as read. Targets without a destination in this project stay no-op stubs.
    [RelayCommand]
    public async Task HandleTapAsync()
    {
        var type = Notification.Type;

        if (type == NotificationType.Message) return; // TODO: Open the message thread once a message page exists.
        else if (type == NotificationType.InviteCodeRequest || type == NotificationType.InviteCodeRequestResult) return; // TODO: Open the invite code request pages once they exist.
        else if (type == NotificationType.Restriction) return; // TODO: Show the restriction notice with the appeal flow.

        if (Notification.Data == null) return;

        if (type == NotificationType.FriendRequest)
        {
            if (!Notification.Data.TryGetValue("UserId", out var userId)) return;

            _ = MarkAsReadAsync();
            _baseViewModel.RequestNavigation(typeof(ProfilePage), userId);
        }
        else
        {
            if (!Notification.Data.TryGetValue("PostId", out var postId)) return;

            var postResult = await _baseViewModel.ExecuteRequestAsync(new GetPost(postId));
            if (!postResult.IsSuccess) return;

            _ = MarkAsReadAsync();
            _baseViewModel.RequestNavigation(typeof(PostPage), postResult.Value);
        }
    }

    // Silent best-effort read: the unread marker clears locally only after the server confirms.
    private async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotifications([Notification.Id]));
        if (success) SetUnread(false);
    }

    // Keeps the DTO and the bindable surface in sync when the notification is marked as read.
    private void SetUnread(bool value)
    {
        Notification.IsUnread = value;
        IsUnread = value;
    }
}
