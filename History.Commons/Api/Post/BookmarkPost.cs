using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class BookmarkPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/bookmark/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public BookmarkPost(string postId) => UrlParameters["postId"] = postId;
}
