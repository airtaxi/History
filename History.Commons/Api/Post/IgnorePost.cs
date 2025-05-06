using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class IgnorePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/ignore/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public IgnorePost(string postId) => UrlParameters["postId"] = postId;
}
