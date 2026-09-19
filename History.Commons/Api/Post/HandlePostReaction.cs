using History.Commons.Enums;
using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class HandlePostReaction : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/reaction/{type}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandlePostReaction(string postId, ReactionType type)
    {
        UrlParameters["postId"] = postId;
        UrlParameters["type"] = type.ToString();
    }
}
