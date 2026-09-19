using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class CancelFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToCancel}/cancel";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public CancelFriendRequest(string userIdToCancel) => UrlParameters["userIdToCancel"] = userIdToCancel;
}
