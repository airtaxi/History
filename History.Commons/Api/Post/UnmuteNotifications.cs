using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class UnmuteNotifications : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/mute-notifications";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnmuteNotifications(string postId) => UrlParameters["postId"] = postId;
}
