using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotificationsByPostId : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications/read-by-post/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ReadNotificationsByPostId(string postId) => UrlParameters["postId"] = postId;
}
