using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Enums;

namespace History.MobileClient.DataTypes;

public class NotificationTypeReadMessage(NotificationType type) : ValueChangedMessage<NotificationType>(type);