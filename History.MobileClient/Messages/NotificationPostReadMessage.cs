using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class NotificationPostReadMessage(string postId) : ValueChangedMessage<string>(postId);
