using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class NotificationPostReadMessage(string postId) : ValueChangedMessage<string>(postId);
