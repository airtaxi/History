using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class NotificationMessageReadMessage(string messageId) : ValueChangedMessage<string>(messageId);
