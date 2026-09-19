using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class DeclineFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToDecline}/decline";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeclineFriendRequest(string userIdToDecline) => UrlParameters["userIdToDecline"] = userIdToDecline;
}
