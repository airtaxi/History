using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class NotificationFriendUserReadMessage(string userId) : ValueChangedMessage<string>(userId);
