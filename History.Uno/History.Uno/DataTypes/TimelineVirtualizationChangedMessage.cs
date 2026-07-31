namespace History.Uno.DataTypes;

public class TimelineVirtualizationChangedMessage(bool isEnabled) : ValueChangedMessage<bool>(isEnabled);