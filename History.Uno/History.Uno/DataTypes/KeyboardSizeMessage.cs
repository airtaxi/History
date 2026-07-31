namespace History.Uno.DataTypes;

public class KeyboardSizeMessage(double keyboardHeight) : ValueChangedMessage<double>(keyboardHeight);