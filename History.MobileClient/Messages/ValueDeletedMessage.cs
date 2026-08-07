using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class ValueDeletedMessage<T>(T value) : ValueChangedMessage<T>(value);
