namespace History.Uno.DataTypes;

public class NotificationFriendUserReadMessage(string userId) : ValueChangedMessage<string>(userId);