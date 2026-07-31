namespace History.Uno.DataTypes;

public class LoadingStateChangedMessage(bool isLoading) : ValueChangedMessage<bool>(isLoading);