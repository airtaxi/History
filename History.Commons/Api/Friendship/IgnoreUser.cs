using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class IgnoreUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/ignore/{userIdToIgnore}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public IgnoreUser(string userIdToIgnore) => UrlParameters["userIdToIgnore"] = userIdToIgnore;
}
