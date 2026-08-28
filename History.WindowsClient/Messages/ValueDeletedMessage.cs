using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class ValueDeletedMessage<T>(T value) : ValueChangedMessage<T>(value);