using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class DiscoverModeChangedMessage(bool isDiscoverMode) : ValueChangedMessage<bool>(isDiscoverMode);
