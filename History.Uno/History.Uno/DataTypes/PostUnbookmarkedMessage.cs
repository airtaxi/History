namespace History.Uno.DataTypes;

public class PostUnbookmarkedMessage(string postId) : ValueChangedMessage<string>(postId);