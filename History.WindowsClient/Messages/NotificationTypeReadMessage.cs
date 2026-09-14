using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Enums;

namespace History.WindowsClient.Messages;

// Broadcasts that the server marked notifications of the given type as read, so the
// notification surfaces can clear their unread markers without a full refresh.
public class NotificationTypeReadMessage(NotificationType type) : ValueChangedMessage<NotificationType>(type);
