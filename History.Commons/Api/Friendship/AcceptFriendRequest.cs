using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class AcceptFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToAccept}/accept";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public AcceptFriendRequest(string userIdToAccept) => UrlParameters["userIdToAccept"] = userIdToAccept;
}
