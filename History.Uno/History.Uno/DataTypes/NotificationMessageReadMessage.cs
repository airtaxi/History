namespace History.Uno.DataTypes;

public class NotificationMessageReadMessage(string messageId) : ValueChangedMessage<string>(messageId);