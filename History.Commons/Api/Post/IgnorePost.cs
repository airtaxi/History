using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class IgnorePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/ignore/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public IgnorePost(string postId) => UrlParameters["postId"] = postId;
}
