using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ReadNotificationsByInviteCodeRequest : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-by-type/invite-code-request";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
}