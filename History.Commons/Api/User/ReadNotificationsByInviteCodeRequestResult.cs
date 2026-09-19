using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ReadNotificationsByInviteCodeRequestResult : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-by-type/invite-code-request-result";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
}