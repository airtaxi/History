using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class MuteNotifications : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/mute-notifications";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public MuteNotifications(string postId) => UrlParameters["postId"] = postId;
}
