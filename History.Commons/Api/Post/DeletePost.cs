using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class DeletePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeletePost(string postId) => UrlParameters["postId"] = postId;
}
