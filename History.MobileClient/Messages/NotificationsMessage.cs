using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Messages;

public class NotificationsMessage(List<NotificationResponseDto> value) : ValueChangedMessage<List<NotificationResponseDto>>(value);
