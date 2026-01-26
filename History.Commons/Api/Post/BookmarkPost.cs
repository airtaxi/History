using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class BookmarkPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/bookmark/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public BookmarkPost(string postId) => UrlParameters["postId"] = postId;
}
