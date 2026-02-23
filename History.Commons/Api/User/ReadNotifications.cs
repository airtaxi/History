using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotifications : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/notifications/read";
    public Method Method => Method.Post;
    public object Body { get; }

    public ReadNotifications(List<string> notificationIds) => Body = new { NotificationIds = notificationIds };
}
