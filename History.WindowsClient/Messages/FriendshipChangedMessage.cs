using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace History.WindowsClient.Messages;

public class FriendshipChangedMessage(string userId, FriendshipStatus? newStatus, UserResponseDto user = null) : ValueChangedMessage<FriendshipChangedData>(FriendshipChangedData.Create(userId, newStatus, user));