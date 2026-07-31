namespace History.Uno.DataTypes;

public class NotificationTypeReadMessage(NotificationType type) : ValueChangedMessage<NotificationType>(type);