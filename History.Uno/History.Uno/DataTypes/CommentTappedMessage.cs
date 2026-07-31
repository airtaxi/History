namespace History.Uno.DataTypes;

public class CommentTappedMessage(UserResponseDto value) : ValueChangedMessage<UserResponseDto>(value);