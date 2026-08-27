using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.MobileClient.Messages;

public class FriendshipChangedMessage(string userId, FriendshipStatus? newStatus, UserResponseDto user = null) : ValueChangedMessage<FriendshipChangedData>(FriendshipChangedData.Create(userId, newStatus, user));
