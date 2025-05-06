using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.DataTypes;

public class CommentTappedMessage(UserResponseDto value) : ValueChangedMessage<UserResponseDto>(value);
