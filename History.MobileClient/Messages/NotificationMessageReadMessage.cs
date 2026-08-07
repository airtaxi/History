using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class NotificationMessageReadMessage(string messageId) : ValueChangedMessage<string>(messageId);
