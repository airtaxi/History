using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class NotificationFriendUserReadMessage(string userId) : ValueChangedMessage<string>(userId);
