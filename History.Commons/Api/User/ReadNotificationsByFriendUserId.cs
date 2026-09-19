using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ReadNotificationsByFriendUserId : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications/read-by-friend/{friendUserId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ReadNotificationsByFriendUserId(string friendUserId) => UrlParameters["friendUserId"] = friendUserId;
}
