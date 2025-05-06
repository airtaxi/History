using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class IgnoreUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/ignore/{userIdToIgnore}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public IgnoreUser(string userIdToIgnore) => UrlParameters["userIdToIgnore"] = userIdToIgnore;
}
