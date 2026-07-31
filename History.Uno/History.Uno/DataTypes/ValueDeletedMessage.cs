namespace History.Uno.DataTypes;

public class ValueDeletedMessage<T>(T value) : ValueChangedMessage<T>(value);