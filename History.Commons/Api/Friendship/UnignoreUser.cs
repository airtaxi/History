using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class UnignoreUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/ignore/{ignoredUserId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnignoreUser(string ignoredUserId) => UrlParameters["ignoredUserId"] = ignoredUserId;
}
