using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class ValueDeletedMessage<T>(T value) : ValueChangedMessage<T>(value);
