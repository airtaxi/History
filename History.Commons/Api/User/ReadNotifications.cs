using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;
using History.Commons.DataTypes.RequestDtos;

namespace History.Commons.Api.User;

public class ReadNotifications : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/notifications/read";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(ReadNotificationsRequestDto));

    public ReadNotifications(List<string> notificationIds) => Body = new ReadNotificationsRequestDto { NotificationIds = notificationIds };
}
