using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class UnmuteNotifications : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/mute-notifications";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnmuteNotifications(string postId) => UrlParameters["postId"] = postId;
}
