using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class PostUnbookmarkedMessage(string postId) : ValueChangedMessage<string>(postId);