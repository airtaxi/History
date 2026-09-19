using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ReadAllNotifications : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-all";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
}
