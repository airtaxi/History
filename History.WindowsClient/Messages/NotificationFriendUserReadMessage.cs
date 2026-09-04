using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class NotificationFriendUserReadMessage(string userId) : ValueChangedMessage<string>(userId);
