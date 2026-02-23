using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadAllNotifications : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-all";
    public Method Method => Method.Post;
}
