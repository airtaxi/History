using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ReadNotificationsByPostId : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications/read-by-post/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ReadNotificationsByPostId(string postId) => UrlParameters["postId"] = postId;
}
