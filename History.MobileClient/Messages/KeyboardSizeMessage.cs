using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class KeyboardSizeMessage(double keyboardHeight) : ValueChangedMessage<double>(keyboardHeight);
