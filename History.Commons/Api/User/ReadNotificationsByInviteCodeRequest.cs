using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotificationsByInviteCodeRequest : IAuthRequiredRequest
{
    public string Path => "/api/user/notifications/read-by-type/invite-code-request";
    public Method Method => Method.Post;
}