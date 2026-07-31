namespace History.Uno.DataTypes;

public class NotificationsMessage(List<NotificationResponseDto> value) : ValueChangedMessage<List<NotificationResponseDto>>(value);