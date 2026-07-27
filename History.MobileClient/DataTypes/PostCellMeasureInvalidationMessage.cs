using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.DataTypes;

public class PostCellMeasureInvalidationMessage(string value) : ValueChangedMessage<string>(value);