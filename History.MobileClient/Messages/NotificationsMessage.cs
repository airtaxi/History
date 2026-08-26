using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.Messages;

public class NotificationsMessage(List<NotificationResponseDto> value) : ValueChangedMessage<List<NotificationResponseDto>>(value);
