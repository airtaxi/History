using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Message;
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

// History notification list item view model: holds the DTO and the display surface used by
// the notifications flyout template.
public partial class HistoryNotificationViewModel : BaseNotificationViewModel, IRecipient<NotificationsReadAllMessage>, IRecipient<NotificationPostReadMessage>, IRecipient<NotificationFriendUserReadMessage>, IRecipient<NotificationTypeReadMessage>
{
    private readonly BaseViewModel _baseViewModel;

    public NotificationResponseDto Notification { get; }

    public override string Title => Notification.Title;
    public override string Body => Notification.Body;
    public override bool IsBodyVisible => !string.IsNullOrEmpty(Notification.Body);
    public override string TimestampText => PostHelper.GenerateFriendlyTimestamp(Notification.CreatedAt, null);
    public override bool IsImageVisible => !string.IsNullOrEmpty(Notification.ImageUrl);

    public override ImageSource ProfileImageSource => Notification.User?.ProfileThumbnailMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(Notification.User.ProfileThumbnailMediaId)));
    public override ImageSource ImageSource => string.IsNullOrEmpty(Notification.ImageUrl) ? null : new BitmapImage(new Uri(Notification.ImageUrl));

    public HistoryNotificationViewModel(NotificationResponseDto notification, BaseViewModel baseViewModel) : base(notification.IsUnread)
    {
        _baseViewModel = baseViewModel;
        Notification = notification;

        WeakReferenceMessenger.Default.Register((IRecipient<NotificationsReadAllMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationPostReadMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationFriendUserReadMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationTypeReadMessage>)this);
    }

    public void Receive(NotificationsReadAllMessage message)
    {
        if (!IsUnread) return;
        SetUnread(false);
    }

    public void Receive(NotificationPostReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Data == null || !Notification.Data.TryGetValue("PostId", out var postId)) return;
        if (postId != message.Value) return;
        SetUnread(false);
    }

    public void Receive(NotificationFriendUserReadMessage message)
    {
        if (!IsUnread) return;
        // A friend-profile visit clears only friend request notifications; other types such as
        // birthday notifications can carry the same UserId in their data.
        if (Notification.Type != NotificationType.FriendRequest) return;
        if (Notification.Data == null || !Notification.Data.TryGetValue("UserId", out var userId)) return;
        if (userId != message.Value) return;
        SetUnread(false);
    }

    // Pages that list a notification type's targets clear that type when they open.
    public void Receive(NotificationTypeReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Type != message.Value) return;
        SetUnread(false);
    }

    // Entry point for notification taps: navigates to the notification target and marks the
    // notification as read. Targets without a destination in this project stay no-op stubs.
    public override async Task HandleTapAsync()
    {
        var type = Notification.Type;

        if (type == NotificationType.Restriction)
        {
            _ = MarkAsReadAsync();
            await RestrictionNoticeHelper.ShowAsync(_baseViewModel, Notification.Body);
            return;
        }
        else if (type == NotificationType.InviteCodeRequest || type == NotificationType.InviteCodeRequestResult) return; // TODO: Open the invite code request pages once they exist.

        if (Notification.Data == null) return;

        if (type == NotificationType.Message)
        {
            if (!Notification.Data.TryGetValue("MessageId", out var messageId)) return;

            var messageResult = await _baseViewModel.ExecuteRequestAsync(new GetMessage(messageId));
            if (!messageResult.IsSuccess) return;

            _ = MarkAsReadAsync();
            var messageViewModel = new HistoryMessageViewModel(messageResult.Value, _baseViewModel);
            await messageViewModel.HandleTapAsync();
        }
        else if (type == NotificationType.FriendRequest)
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
    public override async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotifications([Notification.Id]));
        if (success) SetUnread(false);
    }

    // Keeps the DTO and the bindable surface in sync when the notification is marked as read.
    protected override void OnUnreadStateChanged(bool value) => Notification.IsUnread = value;
}
