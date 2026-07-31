using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class TimelineVirtualizationChangedMessage(bool isEnabled) : ValueChangedMessage<bool>(isEnabled);