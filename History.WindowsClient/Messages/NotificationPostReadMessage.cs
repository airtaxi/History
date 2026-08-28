using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class NotificationPostReadMessage(string postId) : ValueChangedMessage<string>(postId);