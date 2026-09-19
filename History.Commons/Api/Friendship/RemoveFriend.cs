using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class RemoveFriend : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/remove/{userIdToRemove}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public RemoveFriend(string userIdToRemove) => UrlParameters["userIdToRemove"] = userIdToRemove;
}
