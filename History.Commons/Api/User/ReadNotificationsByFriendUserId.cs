using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotificationsByFriendUserId : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications/read-by-friend/{friendUserId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ReadNotificationsByFriendUserId(string friendUserId) => UrlParameters["friendUserId"] = friendUserId;
}
