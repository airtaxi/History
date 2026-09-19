using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class DeletePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeletePost(string postId) => UrlParameters["postId"] = postId;
}
