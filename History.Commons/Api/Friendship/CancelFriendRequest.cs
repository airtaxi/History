using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class CancelFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToCancel}/cancel";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public CancelFriendRequest(string userIdToCancel) => UrlParameters["userIdToCancel"] = userIdToCancel;
}
