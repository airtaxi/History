using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class UnbookmarkPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/bookmark/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnbookmarkPost(string postId) => UrlParameters["postId"] = postId;
}
