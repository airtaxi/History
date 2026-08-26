
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class PostUnbookmarkedMessage(string postId) : ValueChangedMessage<string>(postId);
