using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User
{
    public class UpdatePushNotificationPermission : IAuthRequiredRequest, IRequestWithBody
    {
        public string Path => "/api/user/push-notification-permission";
        public Method Method => Method.Put;
        public object Body { get; }

        public UpdatePushNotificationPermission(PushNotificationType type, AccessPermission accessPermission)
        {
            Body = new UpdatePushNotificationPermissionRequestDto()
            {
                Type = type,
                AccessPermission = accessPermission
            };
        }
    }
}
