using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class MuteNotifications : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/mute-notifications";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public MuteNotifications(string postId) => UrlParameters["postId"] = postId;
}
