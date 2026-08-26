using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class LoadingStateChangedMessage(bool isLoading) : ValueChangedMessage<bool>(isLoading);
