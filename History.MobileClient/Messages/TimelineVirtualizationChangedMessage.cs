using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class TimelineVirtualizationChangedMessage(bool isEnabled) : ValueChangedMessage<bool>(isEnabled);