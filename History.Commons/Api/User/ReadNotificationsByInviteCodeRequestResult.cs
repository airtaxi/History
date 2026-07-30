using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotificationsByInviteCodeRequestResult : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-by-type/invite-code-request-result";
    public Method Method => Method.Post;
}