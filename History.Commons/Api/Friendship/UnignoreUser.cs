using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class UnignoreUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/ignore/{ignoredUserId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnignoreUser(string ignoredUserId) => UrlParameters["ignoredUserId"] = ignoredUserId;
}
