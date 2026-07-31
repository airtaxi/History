namespace History.Uno.DataTypes;

public class UserUnselectedMessage(UserResponseDto user) : ValueDeletedMessage<UserResponseDto>(user);