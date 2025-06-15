using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class WritePublicPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/public-post/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public WritePublicPost(string postId) => UrlParameters["postId"] = postId;
}