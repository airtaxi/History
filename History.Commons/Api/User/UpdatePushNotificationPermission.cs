using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User
{
    public class UpdatePushNotificationPermission : IAuthRequiredRequest, IRequestWithBody
    {
        public string Path => "/api/user/push-notification-permission";
        public HttpRequestMethod Method => HttpRequestMethod.Put;
        public object Body { get; }
        public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdatePushNotificationPermissionRequestDto));

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
