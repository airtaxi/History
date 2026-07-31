namespace History.Uno.DataTypes;

public class NotificationPostReadMessage(string postId) : ValueChangedMessage<string>(postId);